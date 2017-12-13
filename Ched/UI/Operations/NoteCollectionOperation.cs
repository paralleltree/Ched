using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Components.Notes;

namespace Ched.UI.Operations
{
    public abstract class NoteCollectionOperation<T> : IOperation
    {
        protected T Note { get; }
        protected NoteView.NoteCollection Collection { get; }
        public abstract string Description { get; }

        public NoteCollectionOperation(NoteView.NoteCollection collection, T note)
        {
            Collection = collection;
            Note = note;
        }

        public abstract void Undo();
        public abstract void Redo();
    }

    public class InsertTapOperation : NoteCollectionOperation<Tap>
    {
        public override string Description { get { return "TAPの追加"; } }

        public InsertTapOperation(NoteView.NoteCollection collection, Tap note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveTapOperation : NoteCollectionOperation<Tap>
    {
        public override string Description { get { return "TAPの削除"; } }

        public RemoveTapOperation(NoteView.NoteCollection collection, Tap note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertHoldOperation : NoteCollectionOperation<Hold>
    {
        public override string Description { get { return "HOLDの追加"; } }

        public InsertHoldOperation(NoteView.NoteCollection collection, Hold note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveHoldOperation : NoteCollectionOperation<Hold>
    {
        public override string Description { get { return "HOLDの削除"; } }

        public RemoveHoldOperation(NoteView.NoteCollection collection, Hold note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertSlideOperation : NoteCollectionOperation<Slide>
    {
        public override string Description { get { return "SLIDEの追加"; } }

        public InsertSlideOperation(NoteView.NoteCollection collection, Slide note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveSlideOperation : NoteCollectionOperation<Slide>
    {
        public override string Description { get { return "SLIDEの削除"; } }

        public RemoveSlideOperation(NoteView.NoteCollection collection, Slide note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertFlickOperation : NoteCollectionOperation<Flick>
    {
        public override string Description { get { return "FLICKの追加"; } }

        public InsertFlickOperation(NoteView.NoteCollection collection, Flick note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveFlickOperation : NoteCollectionOperation<Flick>
    {
        public override string Description { get { return "FLICKの削除"; } }

        public RemoveFlickOperation(NoteView.NoteCollection collection, Flick note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertAirOperation : NoteCollectionOperation<Air>
    {
        public override string Description { get { return "AIRの追加"; } }

        public InsertAirOperation(NoteView.NoteCollection collection, Air note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveAirOperation : NoteCollectionOperation<Air>
    {
        public override string Description { get { return "AIRの削除"; } }

        public RemoveAirOperation(NoteView.NoteCollection collection, Air note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertAirActionOperation : NoteCollectionOperation<AirAction>
    {
        public override string Description { get { return "AIR-ACTIONの追加"; } }

        public InsertAirActionOperation(NoteView.NoteCollection collection, AirAction note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveAirActionOperation : NoteCollectionOperation<AirAction>
    {
        public override string Description { get { return "AIR-ACTIONの削除"; } }

        public RemoveAirActionOperation(NoteView.NoteCollection collection, AirAction note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }


    public class InsertDamageOperation : NoteCollectionOperation<Damage>
    {
        public override string Description { get { return "ダメージノーツの追加"; } }

        public InsertDamageOperation(NoteView.NoteCollection collection, Damage note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    public class RemoveDamageOperation : NoteCollectionOperation<Damage>
    {
        public override string Description { get { return "ダメージノーツの削除"; } }

        public RemoveDamageOperation(NoteView.NoteCollection collection, Damage note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }
}
