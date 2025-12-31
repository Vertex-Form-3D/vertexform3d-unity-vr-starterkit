using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public abstract class StateMachine : MonoBehaviour
    {
        private readonly Stack<StateType> previousStates = new Stack<StateType>();
        private readonly Dictionary<StateType, State> stateTypeMap = new Dictionary<StateType, State>();

        [SerializeField] protected List<StateType> statesToSkip;

        protected Action<StateType, StateType> StateChanged;
        private StateType currentState;

        protected void Initialize(List<State> states)
        {
            foreach (var state in states)
            {
                stateTypeMap.Add(state.StateType, state);
            }
        }

        public void SetState(StateType stateType)
        {
            var previousState = currentState;
            if (previousState != StateType.None)
            {
                DeactivateState(stateTypeMap[previousState]);

                if (statesToSkip.Contains(stateType))
                {
                    SetState(stateTypeMap[stateType].NextState);
                    return;
                }

                previousStates.Push(previousState);
            }

            currentState = stateType;
            if (stateType != StateType.End)
            {
                ActivateState(stateTypeMap[currentState]);
            }
            StateChanged?.Invoke(currentState, previousState);
        }

        protected void ClearPreviousStates()
        {
            previousStates.Clear();
        }

        public void GoToPreviousState()
        {
            var previousState = currentState;
            if (currentState == StateType.None || previousStates.Count == 0) return;
            
            DeactivateState(stateTypeMap[previousState]);
            currentState = previousStates.Pop();
            ActivateState(stateTypeMap[currentState]);
            StateChanged?.Invoke(currentState, previousState);
        }

        private void ActivateState(State state)
        {
            state.gameObject.SetActive(true);
            state.ActivateState();
        }

        private void DeactivateState(State state)
        {
            state.gameObject.SetActive(false);
            state.DeactivateState();
        }
    }
}
