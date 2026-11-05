// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;

namespace Eryndor.Core.AI
{
    /// <summary>Bir durum. TContext, durumun üzerinde çalıştığı nesnedir (ör. EnemyController).</summary>
    public interface IState<in TContext>
    {
        string Name { get; }
        void Enter(TContext context);
        void Tick(TContext context, float deltaTime);
        void Exit(TContext context);
    }

    /// <summary>
    /// Genel amaçlı sonlu durum makinesi (FSM). Canavarlar bunu kullanır (GDD 5.1).
    /// TimeInState, "5 sn'den fazla takılırsa Devriye'ye dön" gibi fallback kuralları için tutulur.
    /// </summary>
    public sealed class StateMachine<TContext>
    {
        private readonly TContext _context;

        public IState<TContext> Current { get; private set; }
        public float TimeInState { get; private set; }

        /// <summary>(önceki, yeni)</summary>
        public event Action<IState<TContext>, IState<TContext>> StateChanged;

        public StateMachine(TContext context)
        {
            _context = context;
        }

        public void ChangeState(IState<TContext> next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            var previous = Current;
            previous?.Exit(_context);
            Current = next;
            TimeInState = 0f;
            Current.Enter(_context);
            StateChanged?.Invoke(previous, Current);
        }

        public void Tick(float deltaTime)
        {
            if (Current == null) return;
            TimeInState += deltaTime;
            Current.Tick(_context, deltaTime);
        }
    }
}
