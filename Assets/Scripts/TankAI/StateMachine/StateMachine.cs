using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = System.Object;

public class StateMachine 
{  // handles it's own state transitions and updates the current state
   private IState _currentState;
   
   private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type,List<Transition>>(); //all transitions
   private List<Transition> _currentTransitions = new List<Transition>(); //current state's transitions
   private List<Transition> _anyTransitions = new List<Transition>(); //Transitions that can be made from any state
   
   private static List<Transition> EmptyTransitions = new List<Transition>(0);

   public void FrameUpdate()
   {
      var transition = GetTransition();
      if (transition != null) //will only not be null for the first frame of any valid transition condition being true
         SetState(transition.To);
      
      _currentState?.FrameUpdate();
   }

   public void PhysicsUpdate()
   {
      _currentState?.PhysicsUpdate();
   }

   public void SetState(IState state) // runs exit and enter methods for states and updates current state
   {
      if (state == _currentState)
         return;
      
      _currentState?.OnExit();
      _currentState = state;
      
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
}
