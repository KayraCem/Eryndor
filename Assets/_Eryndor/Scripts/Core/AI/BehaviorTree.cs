// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;

namespace Eryndor.Core.AI
{
    public enum NodeStatus { Success, Failure, Running }

    /// <summary>Behavior tree düğümlerinin temeli. Boss (Gorrath) bunu kullanır (GDD 5.2).</summary>
    public abstract class BTNode
    {
        public string Name { get; }
        /// <summary>Son Tick'in sonucu; hata ayıklama ve görselleştirme için.</summary>
        public NodeStatus LastStatus { get; private set; } = NodeStatus.Failure;

        protected BTNode(string name) { Name = name ?? GetType().Name; }

        public NodeStatus Tick(float deltaTime)
        {
            LastStatus = OnTick(deltaTime);
            return LastStatus;
        }

        protected abstract NodeStatus OnTick(float deltaTime);

        /// <summary>Düğüm yarıda kesildiğinde (ör. daha öncelikli bir dal devreye girdi) çağrılır.</summary>
        public virtual void Reset() { }
    }

    /// <summary>
    /// Öncelikli seçici: her Tick'te en baştan başlar, ilk Success ya da Running dönen çocukta durur.
    /// Daha öncelikli bir dal devreye girerse, önceden çalışan dal Reset edilir.
    /// Böylece "can %50'nin altına düştü" gibi bir koşul uzun bir saldırıyı bile kesebilir.
    /// </summary>
    public sealed class Selector : BTNode
    {
        private readonly List<BTNode> _children;
        private BTNode _running;

        public Selector(string name, params BTNode[] children) : base(name)
        {
            _children = new List<BTNode>(children);
        }

        protected override NodeStatus OnTick(float deltaTime)
        {
            foreach (var child in _children)
            {
                var status = child.Tick(deltaTime);
                if (status == NodeStatus.Failure) continue;

                if (_running != null && _running != child) _running.Reset();
                _running = status == NodeStatus.Running ? child : null;
                return status;
            }
            if (_running != null) { _running.Reset(); _running = null; }
            return NodeStatus.Failure;
        }

        public override void Reset()
        {
            _running?.Reset();
            _running = null;
        }
    }

    /// <summary>
    /// Sıra: çocukları sırayla çalıştırır; biri Failure dönerse durur.
    /// Running dönen çocuğu hatırlar ve bir sonraki Tick'te oradan devam eder.
    /// </summary>
    public sealed class Sequence : BTNode
    {
        private readonly List<BTNode> _children;
        private int _index;

        public Sequence(string name, params BTNode[] children) : base(name)
        {
            _children = new List<BTNode>(children);
        }

        protected override NodeStatus OnTick(float deltaTime)
        {
            while (_index < _children.Count)
            {
                var status = _children[_index].Tick(deltaTime);
                if (status == NodeStatus.Running) return NodeStatus.Running;
                if (status == NodeStatus.Failure) { ResetChildren(); return NodeStatus.Failure; }
                _index++;
            }
            ResetChildren();
            return NodeStatus.Success;
        }

        public override void Reset() => ResetChildren();

        private void ResetChildren()
        {
            for (int i = 0; i <= _index && i < _children.Count; i++) _children[i].Reset();
            _index = 0;
        }
    }

    /// <summary>Koşul: true ise Success, değilse Failure.</summary>
    public sealed class Condition : BTNode
    {
        private readonly Func<bool> _predicate;

        public Condition(string name, Func<bool> predicate) : base(name)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        protected override NodeStatus OnTick(float deltaTime) =>
            _predicate() ? NodeStatus.Success : NodeStatus.Failure;
    }

    /// <summary>Eylem: verilen fonksiyonu çalıştırır. İsteğe bağlı onReset, eylem kesildiğinde çağrılır.</summary>
    public sealed class ActionNode : BTNode
    {
        private readonly Func<float, NodeStatus> _action;
        private readonly Action _onReset;

        public ActionNode(string name, Func<float, NodeStatus> action, Action onReset = null) : base(name)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _onReset = onReset;
        }

        protected override NodeStatus OnTick(float deltaTime) => _action(deltaTime);

        public override void Reset() => _onReset?.Invoke();
    }

    /// <summary>Sonucu tersine çevirir (Running aynı kalır).</summary>
    public sealed class Inverter : BTNode
    {
        private readonly BTNode _child;

        public Inverter(string name, BTNode child) : base(name)
        {
            _child = child ?? throw new ArgumentNullException(nameof(child));
        }

        protected override NodeStatus OnTick(float deltaTime)
        {
            var s = _child.Tick(deltaTime);
            if (s == NodeStatus.Success) return NodeStatus.Failure;
            if (s == NodeStatus.Failure) return NodeStatus.Success;
            return NodeStatus.Running;
        }

        public override void Reset() => _child.Reset();
    }
}
