﻿// Copyright (c) Peter Dongan. All rights reserved.
// Licensed under the MIT licence. https://opensource.org/licenses/MIT
// Project: https://github.com/peterdongan/UndoService

namespace StateManagement
{
    public interface IUndoService
    {
        event StateRecordedEventHandler StateRecorded;

        int Id { get; set; }

        bool CanUndo { get; }

        bool CanRedo { get; }

        void RecordState();

        void ClearStacks();

        void ClearUndoStack();

        void Undo();

        void Redo();
    }
}
