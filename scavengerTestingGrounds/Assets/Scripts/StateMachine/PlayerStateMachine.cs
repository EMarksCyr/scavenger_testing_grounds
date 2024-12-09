
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.VFX;

[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))] //good practice to make sure the required components are attached to the object we are controlling, the assigned object not having a character controller, input reader or animator component will result in a runtime error

public class PlayerStateMachine : StateMachine //holds public references to components and members used in the states
{
  
    
    public Vector3 Velocity;
    public float MovementSpeed { get; private set; } = 25f; //adjust character speed here, can't figure out how to make this a serialized field
    public float DashForce { get; private set; } = 5f;
    public float LookRotationDampFactor { get; private set; } = 10f;
    public Transform MainCamera { get; private set; }
    public InputReader InputReader { get; private set; }
    public Animator Animator { get; private set; }
    public CharacterController Controller { get; private set; }

    public AnimationEventReceiver Receiver; //reciever for animation events

    public VisualEffect currentPrefab;

    //public AnimatorStateInfo StateInfo { get; private set; }

    private void Start() //when scene loads
    {
        MainCamera = Camera.main.transform; //assigns references to all components of the game object needed to inact any action in this statemachine

        InputReader = GetComponent<InputReader>();
        Animator = GetComponent<Animator>();
        Controller = GetComponent<CharacterController>();       
        SwitchState(new PlayerMoveState(this)); //assigns PlayerMoveState() as a default state for the gameobject
    }
}

public abstract class PlayerBaseState : State
{
    protected readonly PlayerStateMachine stateMachine;
    
    protected PlayerBaseState(PlayerStateMachine stateMachine) // accepts the PlayerStateMachine and then assigns the reference to the stateMachine
    {
        this.stateMachine = stateMachine;
    }

    protected void CalculateMoveDirection() //calculate the direction of player movement based on the orientation of the camera and input values from InputReader.MoveComposite
    {
        Vector3 cameraForward = new(stateMachine.MainCamera.forward.x, 0, stateMachine.MainCamera.forward.z);
        Vector3 cameraRight = new(stateMachine.MainCamera.right.x, 0, stateMachine.MainCamera.right.z);

        Vector3 moveDirection = cameraForward.normalized * stateMachine.InputReader.MoveComposite.y + cameraRight.normalized * stateMachine.InputReader.MoveComposite.x;

        stateMachine.Velocity.x = moveDirection.x * stateMachine.MovementSpeed; // set the Velocity x and z values to respective values of calculated direction, multiplied by the MovementSpeed.
        stateMachine.Velocity.z = moveDirection.z * stateMachine.MovementSpeed;
    }

    protected void FaceMoveDirection() // rotate the player so it’s always facing the direction of movement
    {
        Vector3 faceDirection = new(stateMachine.Velocity.x, 0f, stateMachine.Velocity.z); //calculates face direction by changes in direction to x or z (not y, obv)

        if (faceDirection == Vector3.zero) //
            return;

        stateMachine.transform.rotation = Quaternion.Slerp(stateMachine.transform.rotation, Quaternion.LookRotation(faceDirection), stateMachine.LookRotationDampFactor * Time.deltaTime);
    }

    protected void Move() // actually move the player using its CharacterController component.
    {
        stateMachine.Controller.Move(stateMachine.Velocity * Time.deltaTime);
    }

   // protected void SpawnParticle()
   // {
    //    VisualEffect currentEffect = Instantiate(currentPrefab, stateMachine.transform.position, stateMachine.transform.rotation)
    //}
}

public class PlayerMoveState : PlayerBaseState
{
    private readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int MoveBlendTreeHash = Animator.StringToHash("MoveBlendTree"); //assigns hash representations of the MoveSpeed and MoveBlendTree parameters from the animator (more performant than string)
    private const float AnimationDampTime = 0.1f;
    private const float CrossFadeDuration = 0.1f; //the time it should take for one animation to transition to the next

    public PlayerMoveState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {

        stateMachine.Animator.CrossFadeInFixedTime(MoveBlendTreeHash, CrossFadeDuration); //fades the animator from the idle animation to the run animation in a fixed time 

        stateMachine.InputReader.OnMainAttackPerformed += SwitchToMainAttackState; //check if attack is pressed and switch to attack state
        stateMachine.InputReader.OnDashPerformed += SwitchToDashState; //check if dash was pressed then switches to dash state
        
    }

    public override void Tick()
    {

        CalculateMoveDirection();
        FaceMoveDirection();
        Move();

        stateMachine.Animator.SetFloat(MoveSpeedHash, stateMachine.InputReader.MoveComposite.sqrMagnitude > 0f ? 1f : 0f, AnimationDampTime, Time.deltaTime); //sets movespeed parameter of animator depending on if the movecomposit is 0 or not, squaring magnitude it is a performant way to check if 0 or not
    }

    public override void Exit()
    {
        stateMachine.InputReader.OnMainAttackPerformed -= SwitchToMainAttackState; //before switching to new state, unsubscribe the switchtodashstate method from the event
        stateMachine.InputReader.OnDashPerformed -= SwitchToDashState; //before switching to new state, unsubscribe the switchtodashstate method from the event 
    }

    private void SwitchToDashState()
    {
        stateMachine.SwitchState(new PlayerDashState(stateMachine));
    }

    private void SwitchToMainAttackState()
    {
        stateMachine.SwitchState(new PlayerMainAttackState(stateMachine));
    }
}

public class PlayerDashState : PlayerBaseState
{
    private readonly int DashHash = Animator.StringToHash("Dash");
    private const float AnimationDampTime = 0.1f;
    private const float CrossFadeDuration = 0.1f; //the time it should take for one animation to transition to the next
    private float DashTime = 0f;
    private float DashTimeLimit = .058f; //duration of time for dash

    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        //Debug.Log("dash pressed"); //so this works fine
        stateMachine.Velocity = new Vector3(stateMachine.Velocity.x* stateMachine.DashForce, stateMachine.Velocity.y, stateMachine.Velocity.z* stateMachine.DashForce); //trying to increase speed in the direction of running by dash force
        stateMachine.Animator.CrossFadeInFixedTime(DashHash, CrossFadeDuration); //crossfades animation to dash animation
    }

    public override void Tick()
    {
        DashTime = DashTime + Time.deltaTime;
        //Debug.Log("dash running" + DashTime);
        if (DashTime >= DashTimeLimit) //seeing if this will stop a dash after 5 frames, need to use time.delta time
        {
            stateMachine.SwitchState(new PlayerMoveState(stateMachine)); // want to go back to move state after dash is done
            DashTime = 0;//need something to mark the time of dash start
        }

        FaceMoveDirection();
        Move();
    }

    public override void Exit() { }
}

// TODO: move
public struct ComboStep
{
    public string animParam;  // Animation parameter
    public int animHash;      // Hash reference to animation
    public float attackAnimEndTime; // Time of the end of the attack animation. Does not include recovery anim. 0 is the start of this combo step.
    public float fullAnimEndTime;  // Time of the full animation including attack + recovery.

    public ComboStep(string animParam, int animHash, float attackEndTime, float fullAnimEndTime)
    {
        Assert.IsTrue(fullAnimEndTime >= attackEndTime);

        this.animParam = animParam;
        this.animHash = animHash;
        this.attackAnimEndTime = attackEndTime;
        this.fullAnimEndTime = fullAnimEndTime;
    }
}

public class PlayerMainAttackState : PlayerBaseState
{
    //private const float AnimationDampTime = 0.1f;
    private const float CrossFadeDuration = 0.05f; //the time it should take for one animation to transition to the next
    //private float animationTime = 0f; //tracks time spent in main attack state
    //private VisualEffect BladeHitEffect;
    //animation event
    //private string Attack; // rename event name in animator and here
    //[Range(0f, 1f)] public float triggerTime;

    //bool isBladeSlashVfxTriggered = false;
    //AnimationEventReceiver receiver;

    public static ComboStep[] comboSteps = {
        new("MainAttack1", Animator.StringToHash("MainAttack1"), 0.72f, 1f),
        new("MainAttack2", Animator.StringToHash("MainAttack2"), 0.30f, 0.45f), // TODO: roughly 0.2f for attack anim
        new("MainAttack3", Animator.StringToHash("MainAttack3"), 0.46f, 0.9f)
    };
    private int curComboStepIdx = 0;
    private bool doNextComboStep = false;
    private float comboStepTime = 0.0f;
    private int todo_debug_tick_counter = 0;
    private bool isFirstTick = true;

    public PlayerMainAttackState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        curComboStepIdx = 0;
        comboStepTime = 0f;
        doNextComboStep = false;
        isFirstTick = true;
        //isBladeSlashVfxTriggered = false;

        FaceMoveDirection();

        // TODO: theory, the first time we hit Enter and Tick, the buttons are considered triggered because it will occur in the same frame, therefore we are counting the same input as effectively two inputs.
        stateMachine.Animator.SetBool(comboSteps[0].animParam, true);
        stateMachine.Animator.CrossFadeInFixedTime(comboSteps[0].animHash, CrossFadeDuration);
        //receiver = stateMachine.Animator.GetComponent<AnimationEventReceiver>(); //get animation reciever for main attack animation
    }

    /*
     * lowerCaseVars
     * CaptialClassesStructures...
     * UpperCaseFunctions
     * m_classMemberVariables
     * s_staticVariables
     * g_globalVariables
     * CONSTANTS
     */

    // NOTE(sdsmith): 
    // TODO
    // IMPORTANT
    // PERFORMANCE
    // DOC

    public override void Tick()
    {
        todo_debug_tick_counter++;

        comboStepTime += Time.deltaTime; // NOTE: A += B => A = A + B
        ref ComboStep curStep = ref comboSteps[curComboStepIdx];

        if (!isFirstTick 
            && comboStepTime <= curStep.attackAnimEndTime 
            && stateMachine.InputReader.IsMainAttackActionTriggered())
        {
            // Queue up the next combo step to be triggered
            doNextComboStep = true;
        }

        if (comboStepTime >= curStep.attackAnimEndTime) {
            bool onFinalComboStep = curComboStepIdx >= comboSteps.Length - 1;
            if (doNextComboStep && !onFinalComboStep)
            {
                // Continue the combo, go to next combo step
                stateMachine.Animator.SetBool(comboSteps[curComboStepIdx].animParam, false);
                curComboStepIdx++;

                curStep = ref comboSteps[curComboStepIdx];
                stateMachine.Animator.SetBool(curStep.animParam, true);
                stateMachine.Animator.CrossFadeInFixedTime(curStep.animHash, CrossFadeDuration);

                comboStepTime = 0.0f;
                doNextComboStep = false;
            }
            else if (comboStepTime >= curStep.fullAnimEndTime)
            {
                // Done full anim, transition to idle state
               
                doNextComboStep = false;
                stateMachine.Animator.SetBool(curStep.animParam, false);
                stateMachine.SwitchState(new PlayerMoveState(stateMachine)); // TODO: memory leak? Are we supposed to being doing new and will the old idle state object get cleaned up??
            }
        }

        // Make sure to set at the end of the tick so it remains true for the
        // whole first tick
        isFirstTick = false;
    }

    public override void Exit() {
    }

    /*
    void NotifyReceiver(Animator animator)
    {
        if (receiver != null)
        {
            receiver.OnAnimationEventTriggered(Attack);
        }
    }
    */
}