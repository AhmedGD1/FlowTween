using System;
using System.Threading;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FlT
{
    public static class TweenAsyncExtensions
    {
        public static TweenAwaitable AwaitAsync(this Tween tween, CancellationToken ct = default)
            => new TweenAwaitable(tween, ct);

        public static TweenAwaiter GetAwaiter(this Tween tween)
            => new TweenAwaiter(tween, default);
    }

    public static class SequenceAsyncExtensions
    {
        public static SequenceAwaitable AwaitAsync(this Sequence sequence, CancellationToken ct = default)
            => new SequenceAwaitable(sequence, ct);

        public static SequenceAwaiter GetAwaiter(this Sequence sequence)
            => new SequenceAwaiter(sequence, default);
    }

    public readonly struct TweenAwaitable
    {
        private readonly Tween             _tween;
        private readonly CancellationToken _ct;

        internal TweenAwaitable(Tween tween, CancellationToken ct)
        {
            _tween = tween;
            _ct    = ct;
        }

        public TweenAwaiter GetAwaiter() => new TweenAwaiter(_tween, _ct);
    }

    public struct TweenAwaiter : INotifyCompletion
    {
        private readonly Tween             _tween;
        private readonly CancellationToken _ct;

        internal TweenAwaiter(Tween tween, CancellationToken ct)
        {
            _tween = tween;
            _ct    = ct;
        }

        public bool IsCompleted => _tween == null || _tween.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            if (_tween == null) { continuation(); return; }

            if (_tween.DbgLoops == -1)
                throw new InvalidOperationException(
                    "[FlowTween] Cannot await a tween with infinite loops (SetLoops(-1)). " +
                    "Use a finite loop count, or don't await the tween — " +
                    "control its lifetime via a CancellationToken instead.");


            Tween             tween = _tween;
            CancellationToken ct    = _ct;

            var state = new TweenAwaitState();

            CancellationTokenRegistration reg = default;
            if (ct.CanBeCanceled)
                reg = ct.Register(static t => ((Tween)t).Kill(), tween);

            tween.OnComplete(() =>
            {
                if (state.Fired) return;
                state.Fired = true;
                reg.Dispose();
                continuation();
            });

            tween.OnKill(() =>
            {
                if (state.Fired) return;
                state.Fired = true;
                reg.Dispose();
                continuation();
            });
        }

        public void GetResult() => _ct.ThrowIfCancellationRequested();
    }

    public readonly struct SequenceAwaitable
    {
        private readonly Sequence          _sequence;
        private readonly CancellationToken _ct;

        internal SequenceAwaitable(Sequence sequence, CancellationToken ct)
        {
            _sequence = sequence;
            _ct       = ct;
        }

        public SequenceAwaiter GetAwaiter() => new SequenceAwaiter(_sequence, _ct);
    }

    public struct SequenceAwaiter : INotifyCompletion
    {
        private readonly Sequence          _sequence;
        private readonly CancellationToken _ct;

        internal SequenceAwaiter(Sequence sequence, CancellationToken ct)
        {
            _sequence = sequence;
            _ct       = ct;
        }

        public bool IsCompleted => _sequence == null || _sequence.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            if (_sequence == null) { continuation(); return; }

            if (_sequence.DbgLoops == -1)
                throw new InvalidOperationException(
                    "[FlowTween] Cannot await a Sequence with infinite loops (SetLoops(-1)). " +
                    "Use a finite loop count, or don't await the sequence — " +
                    "control its lifetime via a CancellationToken instead.");

            Sequence          sequence = _sequence;
            CancellationToken ct       = _ct;

            var state = new TweenAwaitState();

            CancellationTokenRegistration reg = default;
            if (ct.CanBeCanceled)
                reg = ct.Register(static s => ((Sequence)s).Kill(), sequence);

            sequence.OnComplete(() =>
            {
                if (state.Fired) return;
                state.Fired = true;
                reg.Dispose();
                continuation();
            });

            sequence.OnKill(() =>
            {
                if (state.Fired) return;
                state.Fired = true;
                reg.Dispose();
                continuation();
            });
        }

        public void GetResult() => _ct.ThrowIfCancellationRequested();
    }

    internal sealed class TweenAwaitState
    {
        public bool Fired;
    }

    public static class TweenTaskExtensions
    {
        public static async void Forget(this System.Threading.Tasks.Task task)
        {
            try   { await task; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}