using System;
using FracturedChorus.Combat.Block;
using FracturedChorus.Combat.Damage;
using UnityEngine;

namespace FracturedChorus.Combat.Cover
{
    public sealed class CoverRuntime
    {
        public int Gauge { get; private set; }
        public bool IsPending { get; private set; }
        public int ActiveBeatsRemaining { get; private set; }
        public bool IsActive => ActiveBeatsRemaining > 0;
        public float OutgoingDamageMultiplier =>
            IsActive ? CoverConstants.DamageMultiplier : 1f;

        public event Action OnChanged;

        public bool TryCharge(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var before = Gauge;
            Gauge = Mathf.Min(CoverConstants.GaugeCap, Gauge + amount);
            if (Gauge == before)
            {
                return false;
            }

            OnChanged?.Invoke();
            return true;
        }

        public bool CanActivate(bool renAlive) =>
            renAlive &&
            !IsPending &&
            !IsActive &&
            Gauge >= CoverConstants.ActivateCost;

        public bool TryActivate(bool renAlive)
        {
            if (!CanActivate(renAlive))
            {
                return false;
            }

            Gauge -= CoverConstants.ActivateCost;
            IsPending = true;
            Debug.Log(
                $"[Cover] Activated (−{CoverConstants.ActivateCost}) → gauge {Gauge}/{CoverConstants.GaugeCap} pending");
            OnChanged?.Invoke();
            return true;
        }

        public void BeginWindowIfPending()
        {
            if (!IsPending)
            {
                return;
            }

            IsPending = false;
            ActiveBeatsRemaining = CoverConstants.DurationBeats;
            Debug.Log($"[Cover] Window start {ActiveBeatsRemaining} beats ×{CoverConstants.DamageMultiplier}");
            OnChanged?.Invoke();
        }

        public void TickBeat()
        {
            if (ActiveBeatsRemaining <= 0)
            {
                return;
            }

            ActiveBeatsRemaining--;
            if (ActiveBeatsRemaining <= 0)
            {
                Debug.Log("[Cover] Window ended");
            }

            OnChanged?.Invoke();
        }

        public void Reset()
        {
            Gauge = 0;
            IsPending = false;
            ActiveBeatsRemaining = 0;
            OnChanged?.Invoke();
        }

        public BeatTiming RemapPlayerTiming(BeatTiming timing)
        {
            if (!IsActive)
            {
                return timing;
            }

            return timing is BeatTiming.Early or BeatTiming.Late
                ? BeatTiming.OnBeat
                : timing;
        }

        public BlockTiming RemapGuardTiming(BlockTiming timing)
        {
            if (!IsActive)
            {
                return timing;
            }

            return timing is BlockTiming.Early or BlockTiming.Late
                ? BlockTiming.OnBeat
                : timing;
        }
    }
}
