// Copyright (c) Peter Dongan. All rights reserved.
// Licensed under the MIT licence. https://opensource.org/licenses/MIT
// Project: https://github.com/peterdongan/UndoService

using NUnit.Framework;
using StateManagement;
using System;

namespace UndoService.Test
{
    public class UndoServiceTests
    {
        private UndoServiceAggregate _aggregateService;
        private UndoService<int> _individualUndoService;
        private UndoService<int> _undoServiceForInt;
        private IUndoService _subUndoServiceForString;
        private IUndoService _subUndoServiceForInt;
        private string _statefulString;     //(In real use, more complex objects would be used to store state.)
        private int _statefulInt;

        private object _stateSetTag;

        [SetUp]
        public void Setup()
        {
            _undoServiceForInt = new UndoService<int>(GetIntState, SetIntState, 3);
            _individualUndoService = new UndoService<int>(GetIntState, SetIntState, 3);
            _subUndoServiceForInt = new UndoService<int>(GetIntState, SetIntState, 3);
            _subUndoServiceForString = new UndoService<string>(GetStringState, SetStringState, 3);
            IUndoService[] subservices = { _subUndoServiceForInt, _subUndoServiceForString };
            _aggregateService = new UndoServiceAggregate(subservices);
        }

        /// <summary>
        /// Test undo/redo in a single UndoService
        /// </summary>
        [Test]
        public void UndoRedoTest()
        {
            _statefulInt = 1;
            _undoServiceForInt.RecordState();
            _statefulInt = 2;
            _undoServiceForInt.RecordState();
            _undoServiceForInt.Undo();
            Assert.IsTrue(_statefulInt == 1);

            _undoServiceForInt.Redo();
            Assert.IsTrue(_statefulInt == 2);
        }

        /// <summary>
        /// Test that capacity limits are applied to a single UndoService
        /// </summary>
        [Test]
        public void CapacityTest()
        {
            _statefulInt = 1;
            _undoServiceForInt.RecordState();
            _statefulInt = 2;
            _undoServiceForInt.RecordState();
            _statefulInt = 3;
            _undoServiceForInt.RecordState();
            _statefulInt = 4;
            _undoServiceForInt.RecordState();

            _undoServiceForInt.Undo();
            _undoServiceForInt.Undo();
            _undoServiceForInt.Undo();
            Assert.IsTrue(_statefulInt == 1);
            Assert.IsFalse(_undoServiceForInt.CanUndo);

            _undoServiceForInt.Redo();
            _undoServiceForInt.Redo();
            _undoServiceForInt.Redo();
            Assert.IsTrue(_statefulInt == 4);
        }

        /// <summary>
        /// Test undo/redo in an AggregateUndoService
        /// </summary>
        [Test]
        public void AggregateUndoServiceUndoRedoTest()
        {
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 3);

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 2);

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 1);

            _aggregateService.Redo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 2);

            _aggregateService.Redo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 3);

            _aggregateService.Redo();
            Assert.IsTrue(_statefulString.Equals("Two"));
            Assert.IsTrue(_statefulInt == 3);
        }

        /// <summary>
        /// Test that the redo stacks of an aggregate and its subservices are cleared when a new state is recorded in any subservice.
        /// </summary>
        [Test]
        public void AggregateClearRedoOnStateChangeTest()
        {
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 4;
            _subUndoServiceForInt.RecordState();
            _aggregateService.Undo();
            _aggregateService.Undo();
            Assert.IsTrue(_aggregateService.CanRedo);
            Assert.IsTrue(_subUndoServiceForInt.CanRedo);

            _statefulString = "Three";
            _subUndoServiceForString.RecordState();
            Assert.IsTrue(!_aggregateService.CanRedo);
            Assert.IsTrue(!_subUndoServiceForInt.CanRedo);
            
        }

            /// <summary>
            /// Test that AggregateUndoService detects when one of its component UndoServices has nothing left in its undo stack, and that it makes that point the effective end of its own undo stack.
            /// </summary>
            [Test]
        public void AggregateUndoServiceCapacityHandlingTest()
        {
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 4;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Three";
            _subUndoServiceForString.RecordState();

            _aggregateService.Undo();
            _aggregateService.Undo();
            _aggregateService.Undo();
            _aggregateService.Undo();
            Assert.IsFalse(_aggregateService.CanUndo);
            Assert.IsTrue(_statefulInt == 1);
            Assert.IsTrue(_statefulString.Equals("Two"));

            _aggregateService.Redo();
            _aggregateService.Redo();
            _aggregateService.Redo();
            _aggregateService.Redo();
            Assert.IsTrue(_statefulInt == 4);
            Assert.IsTrue(_statefulString.Equals("Three"));
        }

        /// <summary>
        /// Test that individual UndoServices work without anything listening to the StateRecorded event.
        /// </summary>
        [Test]
        public void NoEventHandlerTest()
        {
            _statefulInt = 1;
            _individualUndoService.RecordState();
            _statefulInt = 2;
            _individualUndoService.RecordState();

            _individualUndoService.Undo();
            Assert.IsTrue(_statefulInt == 1);

            _individualUndoService.Redo();
            Assert.IsTrue(_statefulInt == 2);
        }

        /// <summary>
        /// Test that you can undo actions after redoing them in a single UndoService
        /// </summary>
        [Test]
        public void RedoUndoSingleTest()
        {
            _statefulInt = 1;
            _undoServiceForInt.RecordState();
            _statefulInt = 2;
            _undoServiceForInt.RecordState();
            _undoServiceForInt.Undo();
            _undoServiceForInt.Redo();

            Assert.IsTrue(_undoServiceForInt.CanUndo);

            _undoServiceForInt.Undo();
            Assert.IsTrue(_statefulInt == 1);

        }

        /// <summary>
        /// Test that you can undo actions after redoing them in an AggregateUndoService.
        /// </summary>
        [Test]
        public void RedoUndoAggregateTest()
        {
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();

            _aggregateService.Undo();
            _aggregateService.Undo();
            _aggregateService.Undo();
            _aggregateService.Redo();
            _aggregateService.Redo();
            _aggregateService.Redo();

            Assert.IsTrue(_aggregateService.CanUndo);

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 3);

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 2);

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("One"));
            Assert.IsTrue(_statefulInt == 1);

        }

        [Test]
        public void TestSubserviceUndo()
        {
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();
            _statefulString = "Three";
            _subUndoServiceForString.RecordState();

            _aggregateService.Undo();
            Assert.IsTrue(_statefulString.Equals("Two"));

            _subUndoServiceForInt.Undo();
            Assert.IsTrue(_statefulInt == 2);

            _aggregateService.Redo();
            Assert.IsTrue(_statefulInt == 3);
            Assert.IsTrue(_statefulString.Equals("Two"));

            _aggregateService.Redo();
            Assert.IsTrue(_statefulString.Equals("Three"));

        }

        [Test]
        public void TestTagging()
        {
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState("The int was set.");
            _statefulString = "One";
            _subUndoServiceForString.RecordState("The string was set.");
            _aggregateService.StateSet += AggregateService_StateSet;
            
            _aggregateService.Undo();
            Assert.IsTrue(((string)_stateSetTag).Equals("The string was set."));   //Undo will change the string

            _aggregateService.Undo();
            Assert.IsTrue(((string)_stateSetTag).Equals("The int was set."));   //Undo will change the int.

            _aggregateService.Redo();
            Assert.IsTrue(((string)_stateSetTag).Equals("The int was set."));   //Redo will change the int.

            _aggregateService.Redo();
            Assert.IsTrue(((string)_stateSetTag).Equals("The string was set."));   //Redo will change the string
        }

        [Test]
        public void AddSuberviceTest()
        {
            //Add a service to an aggregate after recording state with existing services (verify this works ok)
        }

        [Test]
        public void IsChangedTest()
        {
            var trackedObject = new StatefulClass { TheString = "One", TheInt = 1 };
            var undoService = new UndoService<StatefulClassDto>(trackedObject.GetData, trackedObject.SetData, null);
            Assert.IsTrue(!undoService.IsStateChanged);

            trackedObject.TheString = "Two";
            undoService.RecordState();
            Assert.IsTrue(undoService.IsStateChanged);
            undoService.Undo();
            Assert.IsTrue(!undoService.IsStateChanged);
            undoService.Redo();
            Assert.IsTrue(undoService.IsStateChanged);
            undoService.ClearIsChangedFlag();
            Assert.IsTrue(!undoService.IsStateChanged);
            undoService.Undo();
            Assert.IsTrue(undoService.IsStateChanged);
            undoService.Redo();
            Assert.IsTrue(!undoService.IsStateChanged);

        }

        [Test]
        public void AggregateIsChangedTest()
        {
            Assert.IsTrue(!_aggregateService.IsStateChanged);
            
            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            Assert.IsTrue(_aggregateService.IsStateChanged);


            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _subUndoServiceForInt.Undo();
            Assert.IsTrue(_aggregateService.IsStateChanged);

            _aggregateService.Undo();
            Assert.IsTrue(!_aggregateService.IsStateChanged);

            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _aggregateService.ClearIsChangedFlag();
            Assert.IsTrue(!_aggregateService.IsStateChanged);

            _aggregateService.Undo();
            Assert.IsTrue(_aggregateService.IsStateChanged);

            _aggregateService.Redo();
            Assert.IsTrue(!_aggregateService.IsStateChanged);

        }
        
        [Test]
        public void DirectStackClearExceptionTest()
        {

            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();
            _aggregateService.ClearStacks();

            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();
            _aggregateService.ClearUndoStack();

            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Two";
            _subUndoServiceForString.RecordState();
            _aggregateService.ClearRedoStack();

            _statefulInt = 1;
            _subUndoServiceForInt.RecordState();
            _statefulString = "One";
            _subUndoServiceForString.RecordState();
            _statefulInt = 2;
            _subUndoServiceForInt.RecordState();
            _statefulInt = 3;
            _subUndoServiceForInt.RecordState();
            _statefulString = "Two";

            bool exceptionWasThrown = false;
            try
            {
                _subUndoServiceForString.ClearStacks();
            }
            catch (InvalidOperationException e)
            {
                Assert.IsTrue(e.Message.Equals("A clear stack method was invoked directly on an UndoService that is part of an UndoServiceAggregate. Invoke the clear stack methods on the UndoServiceAggregate instead."));
                exceptionWasThrown = true;
            }
            Assert.IsTrue(exceptionWasThrown);
        }


        private void AggregateService_StateSet(object sender, StateSetEventArgs e)
        {
            _stateSetTag = e.Tag;
        }

        private void GetStringState(out string state)
        {
            state = _statefulString;
        }

        private void SetStringState(string value)
        {
            _statefulString = value;
        }

        private void GetIntState(out int state)
        {
            state = _statefulInt;
        }

        private void SetIntState(int value)
        {
            _statefulInt = value;
        }
    }
}