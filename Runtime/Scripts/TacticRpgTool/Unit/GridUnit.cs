﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.GridData;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.Gameplay;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.AI;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.Gameplay.AilmentSystem;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.Gameplay.Components;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.Gameplay.Extensions;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.Unit.Abilities;
using ProjectCI.CoreSystem.Runtime.TacticRpgTool.Audio;

namespace ProjectCI.CoreSystem.Runtime.TacticRpgTool.Unit
{
    public enum UnitState
    {
        Idle,
        Moving,
        UsingAbility
    }

    public class GridUnit : GridObject
    {
        UnitData m_UnitData;

        UnitState m_CurrentState;
        UnitAbility m_CurrentAbility;

        int m_CurrentMovementPoints;
        int m_CurrentAbilityPoints;

        bool m_bIsTarget = false;
        bool m_bIsMoving = false;
        bool m_bIsAttacking = false;
        bool m_bActivated = false;

        bool m_bIsDead = false;

        UnityEvent OnMovementComplete = new UnityEvent();

        List<LevelCellBase> m_EditedCells = new List<LevelCellBase>();

        public override void Initalize()
        {
            base.Initalize();

            BattleHealth healthComp = gameObject.AddComponent<BattleHealth>();
            if (healthComp)
            {
                healthComp.OnHealthDepleted.AddListener(Kill);
                healthComp.OnHit.AddListener(HandleHit);
                healthComp.OnHeal.AddListener(HandleHeal);
            }
        }

        public override void PostInitalize()
        {
            GetCell().HandleVisibilityChanged();
        }

        public virtual void SelectUnit()
        {
            SetupMovement();
        }
        
        public void CleanUp()
        {
            m_CurrentState = UnitState.Idle;

            foreach (LevelCellBase cell in m_EditedCells)
            {
                if (cell)
                {
                    TacticBattleManager.ResetCellState(cell);
                }
            }

            m_EditedCells.Clear();
        }
        
        public void LookAtCell(LevelCellBase InCell)
        {
            if(InCell && ShouldLookAtTargets())
            {
                gameObject.transform.LookAt(GetCellLookAtPos(InCell));
            }
        }
        
        public void AddAI(UnitAI InAIData)
        {
            UnitAIComponent AIComponent = gameObject.GetComponent<UnitAIComponent>();
            if( !AIComponent )
            {
                AIComponent = gameObject.AddComponent<UnitAIComponent>();
            }

            AIComponent.SetAIData( InAIData );
        }
        
        public void CheckCellVisibility(LevelCellBase InCell)
        {
            if(InCell)
            {
                bool bDead = IsDead();
                bool bCellIsVisible = InCell.IsVisible();

                SetVisible(bCellIsVisible && !bDead);
            }
        }

        public void CheckCellVisibility()
        {
            CheckCellVisibility(GetCell());
        }

        #region Events

        public void BindToOnMovementComplete(UnityAction InAction)
        {
            OnMovementComplete.AddListener(InAction);
        }

        public void UnBindFromOnMovementComplete(UnityAction InAction)
        {
            OnMovementComplete.RemoveListener(InAction);
        }

        #endregion

        #region Setters

        public void SetUnitData(UnitData InUnitData)
        {
            m_UnitData = InUnitData;

            BattleHealth health = GetComponent<BattleHealth>();
            if(health)
            {
                health.SetHealth(m_UnitData.m_Health);
                health.SetArmor(m_UnitData.m_Armor);
                health.SetMagicArmor(m_UnitData.m_MagicalArmor);
            }
        }

        public void SetActivated(bool bInNewActivateState)
        {
            if(m_bActivated != bInNewActivateState)
            {
                m_bActivated = bInNewActivateState;
                if(m_bActivated)
                {
                    HandleActivation();
                }
            }
        }

        public void SetAsTarget(bool bInIsTarget)
        {
            m_bIsTarget = bInIsTarget;
        }

        #endregion

        #region Getters

        public UnitData GetUnitData()
        {
            return m_UnitData;
        }

        public AilmentContainer GetAilmentContainer()
        {
            AilmentContainer ailmentHandler = GetComponent<AilmentContainer>();
            if (!ailmentHandler)
            {
                ailmentHandler = gameObject.AddComponent<AilmentContainer>();
            }

            return ailmentHandler;
        }

        public UnitState GetCurrentState()
        {
            return m_CurrentState;
        }

        public bool IsMoving()
        {
            return m_bIsMoving;
        }

        public bool IsAttacking()
        {
            return m_bIsAttacking;
        }

        public bool IsTarget()
        {
            return m_bIsTarget;
        }

        public bool IsActivated()
        {
            return m_bActivated;
        }

        public bool IsFlying()
        {
            return GetUnitData().m_bIsFlying;
        }

        public bool ShouldLookAtTargets()
        {
            return GetUnitData().m_bLookAtTargets;
        }

        public bool IsDead()
        {
            return m_bIsDead;
        }

        public int GetCurrentMovementPoints()
        {
            return m_CurrentMovementPoints;
        }

        public int GetCurrentAbilityPoints()
        {
            return m_CurrentAbilityPoints;
        }

        public UnitAbilityPlayerData GetUnitAbilityPlayerData(UnitAbility InAbility)
        {
            UnitAbilityPlayerData SelectedPlayerData = new UnitAbilityPlayerData();

            foreach (UnitAbilityPlayerData abilityData in GetUnitData().m_Abilities)
            {
                if (abilityData.unitAbility == InAbility)
                {
                    SelectedPlayerData = abilityData;
                    break;
                }
            }

            return SelectedPlayerData;
        }

        public Vector3 GetCellAllignPos(LevelCellBase InCell)
        {
            if(InCell)
            {
                GridObject Obj = InCell.GetObjectOnCell();
                if (Obj)
                {
                    return InCell.GetAllignPos(Obj);
                }
                else
                {
                    Vector3 AllignPos = InCell.gameObject.transform.position;
                    AllignPos.y = gameObject.transform.position.y;
                    return AllignPos;
                }
            }

            return Vector3.zero;
        }

        Vector3 GetCellLookAtPos(LevelCellBase InCell)
        {
            if(InCell)
            {
                Vector3 allignPos = InCell.GetAllignPos(this);
                allignPos.y = gameObject.transform.position.y;

                return allignPos;
            }

            return Vector3.zero;
        }
        
        #endregion

        #region AbilityStuff

        public List<LevelCellBase> GetAbilityHoverCells(LevelCellBase InCell)
        {
            List<LevelCellBase> outCells = new List<LevelCellBase>();

            if (GetCurrentState() == UnitState.UsingAbility)
            {
                UnitAbility ability = GetCurrentAbility();
                if (ability)
                {
                    List<LevelCellBase> abilityCells = ability.GetAbilityCells(this);
                    List<LevelCellBase> effectedCells = ability.GetEffectedCells(this, InCell);

                    if (abilityCells.Contains(InCell))
                    {
                        foreach (LevelCellBase currCell in effectedCells)
                        {
                            if (currCell)
                            {
                                GameTeam EffectedTeam = (currCell == InCell) ? ability.GetEffectedTeam() : GameTeam.All;

                                if (TacticBattleManager.CanCasterEffectTarget(GetCell(), currCell, EffectedTeam, ability.DoesAllowBlocked()))
                                {
                                    outCells.Add(currCell);
                                }
                            }
                        }
                    }
                }
            }

            return outCells;
        }

        public List<UnitAbilityPlayerData> GetAbilities()
        {
            return new List<UnitAbilityPlayerData>(m_UnitData.m_Abilities);
        }

        public UnitAbility GetCurrentAbility()
        {
            return m_CurrentAbility;
        }

        public void SetupAbility(int abilityIndex)
        {
            if(abilityIndex < m_UnitData.m_Abilities.Length)
            {
                SetupAbility(m_UnitData.m_Abilities[abilityIndex].unitAbility);
            }
        }

        public void SetupAbility(UnitAbility InAbility)
        {
            if (IsMoving() || TacticBattleManager.IsActionBeingPerformed())
            {
                return;
            }

            if (InAbility)
            {
                if (InAbility.GetActionPointCost() <= m_CurrentAbilityPoints)
                {
                    CleanUp();

                    m_CurrentAbility = InAbility;
                    m_CurrentState = UnitState.UsingAbility;

                    List<LevelCellBase> EditedAbilityCells = m_CurrentAbility.Setup(this);
                    m_EditedCells.AddRange(EditedAbilityCells);

                    TacticBattleManager.Get().UpdateHoverCells();
                }
            }
        }

        public void ExecuteAbility(LevelCellBase InCell)
        {
            if( m_CurrentAbility )
            {
                ExecuteAbility(m_CurrentAbility, InCell);
                m_CurrentAbility = null;
            }
            else
            {
                CleanUp();
                HandleAbilityFinished();
            }
        }

        public void ExecuteAbility(UnitAbility InAbility, LevelCellBase InCell)
        {
            if (!IsMoving())
            {
                if (InAbility)
                {
                    m_bIsAttacking = true;

                    UnityEvent OnAbilityComplete = new UnityEvent();
                    OnAbilityComplete.AddListener(HandleAbilityFinished);

                    StartCoroutine( InAbility.Execute(this, InCell, OnAbilityComplete) );
                }
            }

            CleanUp();
        }

        public void RemoveAbilityPoints(int InAbilityPoints)
        {
            m_CurrentAbilityPoints -= InAbilityPoints;
            if (m_CurrentAbilityPoints < 0)
            {
                m_CurrentAbilityPoints = 0;
            }
        }
        
        #endregion

        #region MovementStuff

        public List<LevelCellBase> GetAllowedMovementCells()
        {
            return m_UnitData.m_MovementShape.GetCellList(this, GetCell(), m_CurrentMovementPoints, m_UnitData.m_bIsFlying);
        }

        public void SetupMovement()
        {
            if (IsMoving() || TacticBattleManager.IsActionBeingPerformed())
            {
                return;
            }

            CleanUp();

            if (!m_UnitData.m_MovementShape)
            {
                return;
            }

            m_CurrentState = UnitState.Moving;
            List<LevelCellBase> abilityCells = GetAllowedMovementCells();

            foreach (LevelCellBase cell in abilityCells)
            {
                if (cell && cell.IsVisible())
                {
                    TacticBattleManager.SetCellState(cell, CellState.eMovement);
                }
            }

            m_EditedCells.AddRange(abilityCells);

            TacticBattleManager.Get().UpdateHoverCells();
        }

        public bool ExecuteMovement(LevelCellBase TargetCell, UnityEvent InOnMovementComplete)
        {
            if (IsMoving())
            {
                return false;
            }

            if (!m_UnitData.m_MovementShape || !TargetCell)
            {
                return false;
            }

            if(!TargetCell.IsVisible())
            {
                return false;
            }

            List<LevelCellBase> abilityCells = GetAllowedMovementCells();
            if (!abilityCells.Contains(TargetCell))
            {
                return false;
            }

            InOnMovementComplete.AddListener(HandleMovementFinished);
            TraverseTo(TargetCell, InOnMovementComplete, abilityCells);

            TacticBattleManager.CheckWinConditions();

            CleanUp();
            return true;
        }

        public bool ExecuteMovement(LevelCellBase TargetCell)
        {
            UnityEvent OnMovementComplete = new UnityEvent();
            return ExecuteMovement(TargetCell, OnMovementComplete);
        }

        public void TraverseTo(LevelCellBase InTargetCell, UnityEvent OnMovementComplete = null, List<LevelCellBase> InAllowedCells = null)
        {
            StartCoroutine(EnumeratorTraverseTo(InTargetCell, OnMovementComplete, InAllowedCells));
        }

        public void MoveTo(LevelCellBase InTargetCell)
        {
            StartCoroutine(InternalMoveTo(InTargetCell));
        }

        public IEnumerator EnumeratorTraverseTo(LevelCellBase InTargetCell, UnityEvent OnMovementComplete = null, List<LevelCellBase> InAllowedCells = null)
        {
            if (InTargetCell)
            {
                m_bIsMoving = true;

                PlayAnimation(GetUnitData().m_MovementAnimation);

                TacticBattleManager.AddActionBeingPerformed();

                List<LevelCellBase> cellPath = GetPathTo(InTargetCell, InAllowedCells);

                Vector3 StartPos = GetCell().GetAllignPos(this);

                int MovementCount = 0;

                LevelCellBase FinalCell = InTargetCell;

                LevelCellBase StartingCell = GetCell();

                foreach (LevelCellBase cell in cellPath)
                {
                    FogOfWar fogOfWar = TacticBattleManager.GetFogOfWar();
                    if (fogOfWar)
                    {
                        if (GetTeam() == GameTeam.Friendly)
                        {
                            fogOfWar.CheckPoint(cell);
                        }
                        else
                        {
                            CheckCellVisibility(cell);
                        }
                    }

                    float TimeTo = 0;
                    Vector3 EndPos = cell.GetAllignPos(this);
                    while (TimeTo < 1.5f)
                    {
                        TimeTo += Time.deltaTime * AIManager.GetMovementSpeed();
                        gameObject.transform.position = Vector3.MoveTowards(StartPos, EndPos, TimeTo);

                        LookAtCell(cell);

                        yield return new WaitForSeconds(0.00001f);
                    }

                    gameObject.transform.position = EndPos;
                    StartPos = cell.GetAllignPos(this);
                    yield return new WaitForSeconds(AIManager.GetWaitTime());

                    if ( cell != StartingCell )
                    {
                        AilmentHandler.HandleUnitOnCell(this, cell);
                        PlayTravelAudio();
                    }

                    if ( IsDead() )
                    {
                        break;
                    }

                    if(MovementCount++ >= m_CurrentMovementPoints)
                    {
                        FinalCell = cell;
                        break;
                    }
                }

                if ( !IsDead() )
                {
                    SetCurrentCell(FinalCell);
                    RemoveMovementPoints(cellPath.Count - 1);

                    PlayAnimation(GetUnitData().m_IdleAnimation);
                }

                TacticBattleManager.RemoveActionBeingPerformed();

                m_bIsMoving = false;

                if (OnMovementComplete != null)
                {
                    OnMovementComplete.Invoke();
                }
            }
        }

        public List<LevelCellBase> GetPathTo(LevelCellBase InTargetCell, List<LevelCellBase> InAllowedCells = null)
        {
            AIPathInfo pathInfo = new AIPathInfo();
            pathInfo.StartCell = GetCell();
            pathInfo.TargetCell = InTargetCell;
            pathInfo.bIgnoreUnits = true;
            pathInfo.bTakeWeightIntoAccount = TacticBattleManager.IsTeamAI(GetTeam());
            pathInfo.AllowedCells = InAllowedCells;
            pathInfo.bAllowBlocked = m_UnitData.m_bIsFlying;

            List<LevelCellBase> cellPath = AIManager.GetPath(pathInfo);

            return cellPath;
        }

        IEnumerator InternalMoveTo(LevelCellBase InTargetCell)
        {
            if (InTargetCell)
            {
                TacticBattleManager.AddActionBeingPerformed();

                AIPathInfo pathInfo = new AIPathInfo();
                pathInfo.StartCell = GetCell();
                pathInfo.TargetCell = InTargetCell;
                pathInfo.bIgnoreUnits = true;
                pathInfo.bTakeWeightIntoAccount = false;

                List<LevelCellBase> cellPath = AIManager.GetPath(pathInfo);

                LevelCellBase StartingCell = GetCell();

                Vector3 StartPos = GetCell().GetAllignPos(this);

                SetCurrentCell(InTargetCell);

                foreach (LevelCellBase cell in cellPath)
                {
                    FogOfWar fogOfWar = TacticBattleManager.GetFogOfWar();
                    if (fogOfWar)
                    {
                        if (GetTeam() == GameTeam.Friendly)
                        {
                            fogOfWar.CheckPoint(cell);
                        }
                        else
                        {
                            CheckCellVisibility( cell );
                        }
                    }

                    float TimeTo = 0;
                    Vector3 EndPos = cell.GetAllignPos(this);
                    while (TimeTo < 1.5f)
                    {
                        TimeTo += Time.deltaTime * AIManager.GetMovementSpeed();
                        gameObject.transform.position = Vector3.MoveTowards(StartPos, EndPos, TimeTo);
                        yield return new WaitForSeconds(0.00001f);
                    }

                    gameObject.transform.position = EndPos;
                    StartPos = cell.GetAllignPos(this);
                    yield return new WaitForSeconds(AIManager.GetWaitTime());

                    if ( cell != StartingCell )
                    {
                        AilmentHandler.HandleUnitOnCell(this, cell);
                    }
                }

                TacticBattleManager.RemoveActionBeingPerformed();
            }
        }

        public void RemoveMovementPoints(int InMoveCount)
        {
            m_CurrentMovementPoints -= InMoveCount;
            if(m_CurrentMovementPoints < 0)
            {
                m_CurrentMovementPoints = 0;
            }
        }

        #endregion

        #region Animation

        int CurrPlayClipID = 0;

        Dictionary<int, AnimationClip> m_ClipIdToAnim = new Dictionary<int, AnimationClip>();

        public void PlayAnimation(AnimationClip InClip, bool bInPlayIdleAfter = false)
        {
            if(InClip)
            {

                if ( m_ClipIdToAnim.ContainsKey( CurrPlayClipID ) )
                {
                    if ( m_ClipIdToAnim[ CurrPlayClipID ] == InClip )
                    {
                        return;
                    }
                }

                Animator animator = GetComponent<Animator>();
                if(animator)
                {
                    animator.Play(InClip.name);
                }

                if(bInPlayIdleAfter)
                {
                    StartCoroutine(PlayClipAfterTime(GetUnitData().m_IdleAnimation, InClip.length, ++CurrPlayClipID));
                }
            }
        }

        IEnumerator PlayClipAfterTime(AnimationClip InClip, float InTime, int PlayClipID)
        {
            m_ClipIdToAnim.Add( PlayClipID, InClip );

            yield return new WaitForSeconds(InTime);

            m_ClipIdToAnim.Remove( PlayClipID );

            if ( CurrPlayClipID == PlayClipID )
            {
                if(InClip)
                {
                    Animator animator = GetComponent<Animator>();
                    if (animator)
                    {
                        AnimationClip MovementClip = GetUnitData().m_MovementAnimation;
                        if ( IsMoving() && MovementClip )
                        {
                            animator.Play(GetUnitData().m_MovementAnimation.name);
                        }
                        else
                        {
                            animator.Play( InClip.name );
                        }

                    }
                }
            }
        }

        #endregion

        #region EventListeners

        public virtual void HandleTurnStarted()
        {
            m_CurrentMovementPoints = m_UnitData.m_MovementPoints;
            m_CurrentAbilityPoints = m_UnitData.m_AbilityPoints;
        }

        void HandleAbilityFinished()
        {
            m_bIsAttacking = false;

            GameTeam team = TacticBattleManager.GetUnitTeam(this);
            if( TacticBattleManager.IsTeamHuman(team) && TacticBattleManager.IsPlaying() && !IsDead() )
            {
                SetupMovement();
            }
        }

        void HandleMovementFinished()
        {
            if ( TacticBattleManager.IsPlaying() && !IsDead() )
            {
                SetupMovement();
            }

            OnMovementComplete.Invoke();
        }

        public void Kill()
        {
            CleanUp();

            if ( m_CurrentCell )
            {
                m_CurrentCell.SetObjectOnCell(null);
                m_CurrentCell = null;
            }

            m_bIsDead = true;

            SetVisible( false );

            CheckCellVisibility();

            HandleDeath();

            AudioClip clip = GetUnitData().m_DeathSound;
            if (clip)
            {
                AudioPlayData audioData = new AudioPlayData(clip);
                AudioHandler.PlayAudio(audioData, gameObject.transform.position);
            }

            if (TacticBattleManager.IsActionBeingPerformed())
            {
                TacticBattleManager.BindToOnFinishedPerformedActions(DestroyObj);
            }
            else
            {
                DestroyObj();
            }
        }

        protected virtual void HandleDeath()
        {
            TacticBattleManager.HandleUnitDeath(this);
        }

        void DestroyObj()
        {
            TacticBattleManager.UnBindFromOnFinishedPerformedActions(DestroyObj);
            Destroy(gameObject);
        }

        void HandleHit()
        {
            bool bShowHitAnimationOnMove = TacticBattleManager.GetRules().GetGameplayData().bShowHitAnimOnMove;
            if ( !IsMoving() || bShowHitAnimationOnMove )
            {
                PlayAnimation( GetUnitData().m_DamagedAnimation, true );
            }

            PlayDamagedAudio();
        }

        void HandleHeal()
        {
            PlayAnimation(GetUnitData().m_HealAnimation, true);
            PlayHealAudio();

            AbilityParticle[] particles = GetUnitData().m_SpawnOnHeal;
            foreach (AbilityParticle particle in particles)
            {
                if(particle)
                {
                    Vector3 pos = GetCell().gameObject.transform.position;

                    pos = GetCell().GetAllignPos(this);

                    AbilityParticle CreatedAbilityParticle = Instantiate(particle.gameObject, pos, GetCell().transform.rotation).GetComponent<AbilityParticle>();
                    CreatedAbilityParticle.Setup(null, this, GetCell());
                }
            }
        }

        void PlayDamagedAudio()
        {
            AudioClip clip = GetUnitData().m_DamagedSound;
            if(clip)
            {
                AudioPlayData audioData = new AudioPlayData(clip);
                AudioHandler.PlayAudio(audioData, gameObject.transform.position);
            }
        }

        void PlayHealAudio()
        {
            AudioClip clip = GetUnitData().m_HealSound;
            if (clip)
            {
                AudioPlayData audioData = new AudioPlayData(clip);
                AudioHandler.PlayAudio(audioData, gameObject.transform.position);
            }
        }

        void PlayTravelAudio()
        {
            AudioClip clip = GetUnitData().m_TravelSound;
            if (clip)
            {
                AudioPlayData audioData = new AudioPlayData(clip);
                AudioHandler.PlayAudio(audioData, gameObject.transform.position);
            }
        }

        void HandleActivation()
        {
            TacticBattleManager.HandleUnitActivated(this);
        }

        #endregion

    }
}
