using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = System.Object;

public class StateMachine 
{  // handles it's own state transitions and updates the current state
   public IState _currentState { get; private set; }
   public ISubState _currentSubState { get; private set; }

   private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type,List<Transition>>(); //all transitions
   private List<Transition> _currentTransitions = new List<Transition>(); //current state's transitions
   private List<Transition> _anyTransitions = new List<Transition>(); //Transitions that can be made from any state
   
   private Dictionary<Type, List<ISubState>> _stateToSubstate = new Dictionary<Type, List<ISubState>>(); 
   private Dictionary<Type, SubstateConditions> _substateConditionsMap = new Dictionary<Type, SubstateConditions>();
   private List<ISubState> _currentSubstates = new List<ISubState>();
   
   private static List<Transition> EmptyTransitions = new List<Transition>(0);

   public void FrameUpdate()
   {
      var transition = GetTransition();
      if (transition != null) //will only not be null for the first frame of any valid transition condition being true
         SetState(transition.To);
      CheckSubstateConditions();
      
      _currentState?.FrameUpdate();
      _currentSubState?.FrameUpdate();
   }

   public void PhysicsUpdate()
   {
      _currentState?.PhysicsUpdate();
      _currentSubState?.PhysicsUpdate();
   }

   public void SetState(IState state) // runs exit and enter methods for states and updates current state
   {
      if (state == _currentState)
         return;
      
      if (_currentSubState != null && 
          (!_stateToSubstate.ContainsKey(state.GetType()) ||
           !_stateToSubstate[state.GetType()].Contains(_currentSubState)))
      {
         _currentSubState.OnExit();
         _currentSubState = null;
      }
      _currentState?.OnExit(); // this deliberately should be after the substate check so that 
      // when a state is exited from setstate, onexit can check if the current substate is valid for the next state
      // by just seeing if a substate exists during it's onexit() call
      
      _currentState = state;
      _currentSubstates = _stateToSubstate.TryGetValue(_currentState.GetType(), out var substates) ? substates : new List<ISubState>();
      
      _transitions.TryGetValue(_currentState.GetType(), out _currentTransitions);
      if (_currentTransitions == null)
         _currentTransitions = EmptyTransitions; //Better for memory allocation to use a static list
      
      _currentState.OnEnter();
   }

   public void AddTransition(IState from, IState to, Func<bool> predicate)
   {
      if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
      {
         transitions = new List<Transition>();
         _transitions[from.GetType()] = transitions;
      }
      
      transitions.Add(new Transition(to, predicate));
   }

   public void AddAnyTransition(IState state, Func<bool> predicate)
   {
      _anyTransitions.Add(new Transition(state, predicate));
   }
   
   public void AddSubstate(IState parentState, ISubState subState, Func<bool>[] enterConditions, Func<bool>[] exitConditions)
   {
      if (_stateToSubstate.TryGetValue(parentState.GetType(), out var substates) == false)
      {
         substates = new List<ISubState>();
         _stateToSubstate[parentState.GetType()] = substates;
      }
      
      substates.Add(subState);
      _substateConditionsMap[subState.GetType()] = new SubstateConditions(enterConditions, exitConditions);
   }
   
   public void SetSubstate(ISubState substate)
   {
      if (_currentSubState != null) return;
      
      
      _currentSubState = substate;
      _currentSubState.OnEnter();
   }

   private class Transition
   {
      public Func<bool> Condition {get; }
      public IState To { get; }

      public Transition(IState to, Func<bool> condition)
      {
         To = to;
         Condition = condition;
      }
   }
   
   
   private class SubstateConditions {
      public Func<bool>[] EnterConditions { get; }
      public Func<bool>[] ExitConditions { get; }

      public SubstateConditions(Func<bool>[] enter, Func<bool>[] exit)
      {
         EnterConditions = enter;
         ExitConditions = exit;
      }
      
   }

   private Transition GetTransition()
   {
      foreach(var transition in _anyTransitions)
         if (transition.Condition())
            return transition;
      
      foreach (var transition in _currentTransitions)
         if (transition.Condition())
            return transition;

      return null;
   }
   
   
   
   public void CheckSubstateConditions()
   {
      if (_currentSubState == null)
      {
         foreach (var substate in _currentSubstates)
         {
            if (_substateConditionsMap.TryGetValue(substate.GetType(), out var conditions))
            {
               foreach (var enterCond in conditions.EnterConditions)
               {
                  if (enterCond())
                  {
                     bool exit = false;
                     foreach (var exitCond in conditions.ExitConditions)
                     {
                        if (exitCond())
                        {
                           exit = true;
                           break;
                        }
                     }
                     if (!exit)
                     {
                        SetSubstate(substate);
                        break;
                     }
                  }
               }
            }
         }
      }
      else
      {
         var substateType = _currentSubState.GetType();
         if (_substateConditionsMap.TryGetValue(substateType, out var conditions))
         {
            foreach (var exitCond in conditions.ExitConditions)
            {
               if (exitCond()) //if exit condition is true, exit the substate
               {
                  ExitSubstate();
                  break;
               }
            }
         }
      }
   }
   
   public void ExitSubstate()
   {
      _currentSubState.OnExit();
      _currentSubState = null;
   }
   
   
}
