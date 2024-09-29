using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FirstPersonMovement : MonoBehaviour
{
    #region VARIABLES
    [SerializeField]
    public int CurrHealth = 3; // the health system. Three strikes and yer out
    [SerializeField]
    public float stamina = 100; // stamina is 100
    [HideInInspector]
    public const float MAXSTAMINA = 100; // this variable NEVER changes.
    public float chargeStamina = 15; // I made this public and visible cuz it keeps going to 0? Odd.
    [HideInInspector]
    public float runCost = 20; // this is how much stamina it costs to run.
    [HideInInspector]
    public float speed = 5; // this is the walking speed
    [HideInInspector]
    public float runSpeed = 9; // this is the running speed.
    public int noiseLvl; // this is how much noise they make, it determines if the monster "hears" the player by checking if this variable is larger than their distance. Cuz if it is, then they should hear them.
    [Header("Running")]
    public bool canRun = true; // determines if the player can run
    // these determine if the player is running, walking, or crouched. And have getters and setters.
    public bool IsRunning { get; private set; }
    public bool IsWalking { get; private set; }
    public bool IsCrouched {  get; private set; }
    private Coroutine recharge; // this is set up so that the player recharges their stamina when they're not running.
    // this represents the stamina bar in the UI.
    public Image StaminaBar;
    //private Coroutine recharge;
    // these determine what keys are being pressed by the player.
    public KeyCode runningKey = KeyCode.LeftShift;
    public KeyCode[] walkKey = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    public KeyCode crouchKey = KeyCode.LeftControl;

    Rigidbody rigidbody;
    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();
    #endregion
    void Awake()
    {
        // Get the rigidbody on this.
        rigidbody = GetComponent<Rigidbody>();
    }
    // decrements the player's health whenever they take damage, right after it checks for health.
    public void takeDamage()
    {
        if(CurrHealth > 0)
        {
            CurrHealth--;
            checkHP();
        }
    }
    // this checks the player's HP. if its 0 (or less than that somehow), then the player object is set to false. NOTE: DOESN'T DO SCENE TRANSITIONS YET!
    private void checkHP()
    {
        if(CurrHealth <= 0)
        {
            gameObject.SetActive(false);
        }
        return;
    }
    // this is only for trap collisions, not monster attacks.
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Trap")) // if we collide with a trap tag, we take damage.
        {
            takeDamage();
        }
    }
    // fixed update for better performance.
    void FixedUpdate()
    {
        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey); // determines if we're running.
        IsWalking = !IsRunning && (Input.GetKey(walkKey[0]) || Input.GetKey(walkKey[1]) || Input.GetKey(walkKey[2]) || Input.GetKey(walkKey[3])); // this determines if the player is moving at all, or rather, walking
        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed; // determines the player's speed based on if they're running, and if so, we set their speed to runSpeed, otherwise its just speed.
        if (speedOverrides.Count > 0) // if the speed override count is greater than 0, then we modify the target speed by decrementing the count for overrides by 1.
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }
        #region STAMINA
        if (IsRunning) // if they're running, then we drain the stamina bar by the runCost, and the deltaTime.
        {
            stamina -= runCost * Time.deltaTime;
            if (stamina < 0) stamina = 0; // if the stamina goes below 0, we set it to 0
            StaminaBar.fillAmount = stamina / MAXSTAMINA; // we fill (or rather drain) it dividing the stamina by MAXSTAMINA
            if (recharge != null) StopCoroutine(recharge); // if our recharge stamina isn't null, then we stop recharging our stamina. I used a variable so it doesn't recharge whilst we're still running. Otherwise allowing for infinite sprinting
            recharge = StartCoroutine(RechargeStamina()); // we start recharging our stamina.
        }
        if (stamina == 0) // if stamina is 0, then we can't run
        {
            canRun = false;
        }
        else if(stamina > 1) // if our stamina is greater than 1, we can run!
        {
            canRun = true;
        }
        #endregion
        #region NOISE_LEVEL
        // this segment checks if the player's running, crouched, or just walking normally. Depending on the state, the noise level changes. The higher the noise level, the higher the likelihood
        // of alerting the monster based on distance (so that means it can be decently far away, and it'll be alerted to the noise even if it has no line of sight, but it'll investigate).
        if (IsRunning && !IsCrouched)
        {
            noiseLvl = 50; // feel free to adjust these values! The 50 is high because I did it for testing purposes.
        }
        else if(IsWalking && !IsRunning) // if they're walking, but not running, then we know they're either crouched, or just walking normally
        {
            if(IsCrouched) // if they're crouched, we set it to 3
            {
                noiseLvl = 3;
            }
            else // otherwise its normal walking, so its 6.
            {
                noiseLvl = 6;
            }
        }
        else // if the player ain't moving, its making no noise.
        {
            noiseLvl = 0;
        }
        #endregion
        // Get targetVelocity from input.
        Vector2 targetVelocity =new Vector2( Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.
        rigidbody.velocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.velocity.y, targetVelocity.y);
    }
    // this coroutine determines if we should recharge our stamina. We first wait for 1 second before regenerating stamina.
    #region STAMINA_COROUTINE
    private IEnumerator RechargeStamina()
    {
        yield return new WaitForSeconds(1f); // wait for 1 second before we start recharging stamina
        while(stamina < MAXSTAMINA) // if our stamina is below the MAX threshhold, we then need to regen the stamina.
        {
            stamina += chargeStamina / 10f; // we regenerate our stamina by 20 and divide by 10. 
            if (stamina > MAXSTAMINA)
            {
                stamina = MAXSTAMINA; // set the stamina to MAXSTAMINA if we go above it.
            }
            StaminaBar.fillAmount = stamina / MAXSTAMINA; // we fill the stamina bar by the stamina percentage (aka, stamina/MAXSTAMINA)
            yield return new WaitForSeconds(.1f); // we regenerate 1 stamina (I think) every .1 second.
        }
    }
    #endregion
}