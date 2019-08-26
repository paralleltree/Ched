using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Events;

namespace Ched.UI.Operations
{
    public abstract class EventCollectionOperation<T> : IOperation where T : EventBase
    {
        protected T Event { get; }
        protected List<T> Collection { get; }
        public abstract string Description { get; }

        public EventCollectionOperation(List<T> collection, T item)
        {
            Collection = collection;
            Event = item;
        }

        public abstract void Redo();
        public abstract void Undo();
    }

    public class InsertEventOperation<T> : EventCollectionOperation<T> where T : EventBase
    {
        public override string Description { get { return "イベントの挿入"; } }

        public InsertEventOperation(List<T> collection, T item) : base(collection, item)
        {
        }

        public override void Redo()
        {
            Collection.Add(Event);
        }

        public override void Undo()
        {
            Collection.Remove(Event);
        }
    }

    public class RemoveEventOperation<T> : EventCollectionOperation<T> where T : EventBase
    {
        public override string Description { get { return "イベントの削除"; } }

        public RemoveEventOperation(List<T> collection, T item) : base(collection, item)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Event);
        }

        public override void Undo()
        {
            Collection.Add(Event);
        }
    }
}
