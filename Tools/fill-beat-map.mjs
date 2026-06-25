import fs from "fs";
import path from "path";
import { spawnSync } from "child_process";
import ffmpegStatic from "ffmpeg-static";

const ROOT = path.resolve(import.meta.dirname, "..");
const MP3 = path.join(ROOT, "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3");
const CSV = path.join(ROOT, "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv");
const BAK = CSV.replace(".csv", ".prefill.bak.csv");

const SAMPLE_RATE = 22050;
const HOP = 512;
const WIN = 2048;

const COVER_WINDOW = 0.085;
const MIN_NEW_GAP = Number(process.env.MIN_NEW_GAP ?? 0.07);
const STRENGTH_PERCENTILE = Number(process.env.STRENGTH_PCT ?? 0.45);
const PROTECT_QUIET = process.env.PROTECT_QUIET !== "0";
const QUIET_GAP = Number(process.env.QUIET_GAP ?? 1.0);
const DRY_RUN = process.argv.includes("--dry");

function readCsv(filePath) {
  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/).filter(Boolean);
  const beats = [];
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    if (i === 0 && line.toLowerCase().includes("time")) continue;
    const parts = line.split(",");
    if (parts.length >= 2) {
      const t = parseFloat(parts[1].trim());
      if (!Number.isNaN(t)) beats.push(t);
    }
  }
  return beats;
}

function writeCsv(filePath, beats) {
  const rows = ["beat,time_sec"];
  for (let i = 0; i < beats.length; i++) rows.push(`${i},${beats[i].toFixed(4)}`);
  fs.writeFileSync(filePath, rows.join("\n") + "\n", "utf8");
}

function decodeMp3() {
  const ff = spawnSync(
    ffmpegStatic,
    ["-i", MP3, "-f", "f32le", "-ac", "1", "-ar", String(SAMPLE_RATE), "-"],
    { encoding: "buffer", maxBuffer: 256 * 1024 * 1024 }
  );
  if (ff.status !== 0) throw new Error(ff.stderr?.toString() || "ffmpeg decode failed");
  const buf = ff.stdout;
  return new Float32Array(buf.buffer, buf.byteOffset, buf.byteLength / 4);
}

function computeOnsets(samples) {
  const frameCount = Math.floor((samples.length - WIN) / HOP);
  const flux = new Float64Array(frameCount);
  let prevLow = 0;
  let prevHigh = 0;

  for (let f = 0; f < frameCount; f++) {
    const start = f * HOP;
    let low = 0;
    let high = 0;
    for (let n = 0; n < WIN; n++) {
      const s = samples[start + n] ?? 0;
      low += Math.abs(s);
      if (n > 0) high += Math.abs(s - (samples[start + n - 1] ?? 0));
    }
    flux[f] = Math.max(0, low - prevLow) * 0.75 + Math.max(0, high - prevHigh) * 0.25;
    prevLow = low;
    prevHigh = high;
  }

  const smooth = new Float64Array(frameCount);
  for (let i = 0; i < frameCount; i++) {
    let s = 0;
    let c = 0;
    for (let j = i - 2; j <= i + 2; j++) {
      if (j >= 0 && j < frameCount) {
        s += flux[j];
        c++;
      }
    }
    smooth[i] = s / c;
  }

  const localMean = new Float64Array(frameCount);
  const radius = 40;
  for (let i = 0; i < frameCount; i++) {
    let s = 0;
    let c = 0;
    for (let j = Math.max(0, i - radius); j <= Math.min(frameCount - 1, i + radius); j++) {
      s += smooth[j];
      c++;
    }
    localMean[i] = s / c;
  }

  const peaks = [];
  for (let i = 2; i < frameCount - 2; i++) {
    const v = smooth[i];
    if (v > smooth[i - 1] && v > smooth[i + 1] && v > localMean[i] * 1.3 && v > 0.0004) {
      peaks.push({ time: (i * HOP) / SAMPLE_RATE, strength: v });
    }
  }
  peaks.sort((a, b) => a.time - b.time);
  return peaks;
}

function hasBeatNear(beats, t, window) {
  let lo = 0;
  let hi = beats.length - 1;
  while (lo <= hi) {
    const mid = (lo + hi) >> 1;
    if (beats[mid] < t - window) lo = mid + 1;
    else if (beats[mid] > t + window) hi = mid - 1;
    else return true;
  }
  return false;
}

function percentile(values, p) {
  const s = values.slice().sort((a, b) => a - b);
  return s[Math.min(s.length - 1, Math.floor(s.length * p))];
}

function main() {
  if (!fs.existsSync(CSV)) throw new Error("CSV not found");
  const beats = readCsv(CSV);
  console.log("Current beats:", beats.length);

  console.log("Decoding MP3...");
  const samples = decodeMp3();
  console.log("Detecting onsets...");
  const peaks = computeOnsets(samples);
  console.log("Onset peaks:", peaks.length);

  const matchedStrengths = [];
  for (const p of peaks) {
    if (hasBeatNear(beats, p.time, COVER_WINDOW)) matchedStrengths.push(p.strength);
  }
  const threshold = matchedStrengths.length
    ? percentile(matchedStrengths, STRENGTH_PERCENTILE)
    : 0;
  console.log("Strength threshold (matched p" + STRENGTH_PERCENTILE * 100 + "):", threshold.toFixed(5));

  const quietRanges = [];
  if (PROTECT_QUIET) {
    for (let i = 1; i < beats.length; i++) {
      if (beats[i] - beats[i - 1] > QUIET_GAP) {
        quietRanges.push([beats[i - 1], beats[i]]);
      }
    }
  }
  const inQuietRange = (t) =>
    quietRanges.some(([a, b]) => t > a + COVER_WINDOW && t < b - COVER_WINDOW);

  const candidates = [];
  let skippedQuiet = 0;
  for (const p of peaks) {
    if (p.strength < threshold) continue;
    if (hasBeatNear(beats, p.time, COVER_WINDOW)) continue;
    if (inQuietRange(p.time)) {
      skippedQuiet++;
      continue;
    }
    candidates.push(p);
  }
  candidates.sort((a, b) => a.time - b.time);
  if (PROTECT_QUIET) {
    console.log("Protected quiet gaps (>" + QUIET_GAP + "s):", quietRanges.length, "| onsets skipped inside them:", skippedQuiet);
  }

  const merged = beats.slice();
  const inserted = [];
  for (const c of candidates) {
    if (hasBeatNear(merged, c.time, MIN_NEW_GAP)) continue;
    merged.push(c.time);
    merged.sort((a, b) => a - b);
    inserted.push(c.time);
  }

  console.log("Missing strong onsets found:", candidates.length);
  console.log("Beats inserted:", inserted.length);
  console.log("New total:", merged.length);

  const biggestGaps = [];
  for (let i = 1; i < beats.length; i++) {
    const gap = beats[i] - beats[i - 1];
    biggestGaps.push({ gap, from: beats[i - 1], to: beats[i] });
  }
  biggestGaps.sort((a, b) => b.gap - a.gap);
  console.log("Largest original gaps (top 8):");
  for (const g of biggestGaps.slice(0, 8)) {
    console.log(`  ${g.from.toFixed(3)}s -> ${g.to.toFixed(3)}s  gap ${g.gap.toFixed(3)}s`);
  }

  if (inserted.length) {
    console.log("Sample inserted times (first 15):");
    console.log("  " + inserted.slice(0, 15).map((t) => t.toFixed(3)).join(", "));
  }

  if (DRY_RUN) {
    console.log("DRY RUN - no file written.");
    return;
  }

  if (!fs.existsSync(BAK)) {
    fs.copyFileSync(CSV, BAK);
    console.log("Backup:", BAK);
  }
  writeCsv(CSV, merged);
  console.log("Wrote:", CSV);
}

main();
