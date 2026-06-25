import fs from "fs";
import path from "path";
import { spawnSync } from "child_process";
import ffmpegStatic from "ffmpeg-static";

const ROOT = path.resolve(import.meta.dirname, "..");
const MP3 = path.join(ROOT, "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3");
const CSV = path.join(ROOT, "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv");
const BAK = CSV.replace(".csv", ".user.bak.csv");
const SOURCE = process.argv.includes("--from-backup") && fs.existsSync(BAK) ? BAK : CSV;

const SAMPLE_RATE = 22050;
const HOP = 512;
const WIN = 2048;
const SNAP_WINDOW = 0.08;
const MIN_SNAP_OFFSET = 0.045;
const MAX_SNAP_SHIFT = 0.11;
const MIN_BEAT_GAP = 0.005;

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
  for (let i = 0; i < beats.length; i++) {
    rows.push(`${i},${beats[i].toFixed(4)}`);
  }
  fs.writeFileSync(filePath, rows.join("\n") + "\n", "utf8");
}

function decodeMp3() {
  const ff = spawnSync(
    ffmpegStatic,
    ["-i", MP3, "-f", "f32le", "-ac", "1", "-ar", String(SAMPLE_RATE), "-"],
    { encoding: "buffer", maxBuffer: 256 * 1024 * 1024 }
  );
  if (ff.status !== 0) {
    throw new Error(ff.stderr?.toString() || "ffmpeg decode failed");
  }
  const buf = ff.stdout;
  const samples = new Float32Array(buf.buffer, buf.byteOffset, buf.byteLength / 4);
  return samples;
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
      const a = Math.abs(s);
      low += a;
      if (n > 0) {
        high += Math.abs(s - (samples[start + n - 1] ?? 0));
      }
    }
    const drumFlux = Math.max(0, low - prevLow);
    const transFlux = Math.max(0, high - prevHigh);
    flux[f] = drumFlux * 0.75 + transFlux * 0.25;
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

  const peaks = [];
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

  for (let i = 2; i < frameCount - 2; i++) {
    const v = smooth[i];
    if (
      v > smooth[i - 1] &&
      v > smooth[i + 1] &&
      v > localMean[i] * 1.35 &&
      v > 0.0005
    ) {
      peaks.push({ frame: i, time: (i * HOP) / SAMPLE_RATE, strength: v });
    }
  }

  peaks.sort((a, b) => a.time - b.time);
  return peaks;
}

function nearestOnset(peaks, t, window) {
  let best = null;
  let bestDist = Infinity;
  for (const p of peaks) {
    const d = Math.abs(p.time - t);
    if (d <= window && d < bestDist) {
      bestDist = d;
      best = p;
    }
  }
  return best;
}

function mergeTooClose(beats) {
  return { beats: beats.slice(), merged: 0 };
}

function alignBeats(beats, peaks) {
  const aligned = beats.slice();
  const stats = { snapped: 0, kept: 0, forcedZero: 0 };

  aligned[0] = 0;
  stats.forcedZero = beats[0] !== 0 ? 1 : 0;

  for (let i = 1; i < aligned.length; i++) {
    const t = aligned[i];
    const on = nearestOnset(peaks, t, SNAP_WINDOW);
    if (!on) {
      stats.kept++;
      continue;
    }
    const offset = Math.abs(on.time - t);
    if (offset >= MIN_SNAP_OFFSET && offset <= MAX_SNAP_SHIFT) {
      aligned[i] = on.time;
      stats.snapped++;
    } else {
      stats.kept++;
    }
  }

  return { aligned, stats };
}

function measureError(beats, peaks) {
  let sum = 0;
  let count = 0;
  let max = 0;
  for (const t of beats) {
    const on = nearestOnset(peaks, t, SNAP_WINDOW);
    if (on) {
      const e = Math.abs(on.time - t);
      sum += e;
      count++;
      if (e > max) max = e;
    }
  }
  return { avgMs: count ? (sum / count) * 1000 : 0, maxMs: max * 1000, matched: count };
}

function main() {
  if (!fs.existsSync(SOURCE)) throw new Error("CSV not found");
  if (SOURCE === CSV && !fs.existsSync(BAK)) {
    fs.copyFileSync(CSV, BAK);
    console.log("Backup:", BAK);
  }

  const original = readCsv(SOURCE);
  console.log("Source:", SOURCE);
  console.log("Input beats:", original.length);
  console.log("Beat 0 before:", original[0].toFixed(4));
  console.log("Beat last before:", original[original.length - 1].toFixed(4));

  console.log("Decoding MP3...");
  const samples = decodeMp3();
  console.log("Samples:", samples.length, "duration:", (samples.length / SAMPLE_RATE).toFixed(2), "s");

  console.log("Detecting onsets...");
  const peaks = computeOnsets(samples);
  console.log("Onset peaks:", peaks.length);

  const beforeErr = measureError(original, peaks);
  console.log("Before align - avg error:", beforeErr.avgMs.toFixed(1), "ms, max:", beforeErr.maxMs.toFixed(1), "ms");

  let beats = original.slice();
  beats[0] = 0;

  const { aligned, stats } = alignBeats(beats, peaks);
  const merged = mergeTooClose(aligned, peaks);
  const final = merged.beats;

  const afterErr = measureError(final, peaks);
  console.log("After align - avg error:", afterErr.avgMs.toFixed(1), "ms, max:", afterErr.maxMs.toFixed(1), "ms");
  console.log("Snapped:", stats.snapped, "Kept:", stats.kept, "Merged:", merged.merged, "Final count:", final.length);

  writeCsv(CSV, final);
  console.log("Wrote:", CSV);

  const worst = [];
  for (let i = 0; i < final.length; i++) {
    const on = nearestOnset(peaks, final[i], SNAP_WINDOW);
    if (on) {
      const e = Math.abs(on.time - final[i]) * 1000;
      if (e > 25) worst.push({ i, t: final[i], errMs: e.toFixed(1) });
    }
  }
  worst.sort((a, b) => b.errMs - a.errMs);
  console.log("Remaining >25ms:", worst.length);
  if (worst.length) {
    console.log("Top 10 worst:");
    for (const w of worst.slice(0, 10)) {
      console.log(`  beat ${w.i} @ ${w.t.toFixed(4)}s err ${w.errMs}ms`);
    }
  }
}

main();
