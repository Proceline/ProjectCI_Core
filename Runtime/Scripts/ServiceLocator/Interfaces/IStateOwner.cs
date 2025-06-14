﻿using System;

namespace ProjectCI.CoreSystem.Runtime.States.Interfaces
{
    public interface IStateOwner<T> where T : Enum
    {
        T GetCurrentState();
        void AddState(T state);
        void RemoveLastState();
        void ClearStates();
    }
}
