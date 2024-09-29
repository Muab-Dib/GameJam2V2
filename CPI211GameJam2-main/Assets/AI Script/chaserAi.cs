using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
public class chaserAi : MonoBehaviour
{
    #region VARIABLES
    public AudioSource scary;
    public NavMeshAgent ai;
    public List<Transform> destinations; //our list of nodes which you can manually drag into the unity editor when you set up a node.
    public Animator aiAnim;
    [HideInInspector] // We hide all these variables from the inspector as we don't need the player to see em, and so its more readable. DOWNSIDE: If the AI breaks, its because some of the variables here were reset. So, don't reset em.
    public float walkSpeed, chaseSpeed, // determines the default speeds
        minIdleTime = 2, maxIdleTime = 9, idleTime, raycastDistance, // determines how long it'll be idle for, and how far it can see out
        catchDistance, chaseTime, minChaseTime = 10, maxChaseTime = 20, // this determines how close the monster can be before it can hit the player, and how long it'll chase the player when the player loses its sight (aka, how long the player must remain
        // outside its LoS so the chase will end)
        jumpscareTime, dist, // the dist determines the distance from the monster and the player
        investigateTime, minInvestigateTime = 5, maxInvestigateTime = 10, // this indicates how long the monster will be interested in investigating a noise before it returns to its original pathing
        recentAtkTime, cooldown = 3; // these last two track when the monster's most recent attack was, and the cooldown between each attack before it can deal damage again
    [HideInInspector]
    public bool walking, chasing, investigating; // determines its states
    public Transform player; // this represents the player
    private int PlayerNoiseLvl; // this determmines how loud the player is.
    private Vector3 lastKnownLoc; // this is to store the player's position when the monster detects noise.
    [HideInInspector]
    Transform currentDest;
    [HideInInspector]
    Vector3 dest; // destionation postion.
    [HideInInspector]
    int destinationChoice; //self explanatory
    [HideInInspector]
    public int destinationAmount=6; // the amount of nodes on the map that the monster can choose.
    [HideInInspector]
    public Vector3 rayCastOffset;
    public string deathScene; //insert scene name into field to load scene.
    #endregion
    private void Start()
    {
        walking = true; // its default state is walking when we start
        destinationChoice = Random.Range(0, destinationAmount); //monster choosing node
        currentDest = destinations[destinationChoice]; //setting that node to the monsters destination
        
    }
    private void FixedUpdate()
    {
        Vector3 direction = (player.position - transform.position).normalized; // we get the direction of the monster
        PlayerNoiseLvl = player.root.GetComponent<FirstPersonMovement>().noiseLvl; // Get the noise level from the player class.
        dist = Vector3.Distance(player.position, transform.position); // we determine the distance from the player and the monster
        RaycastHit hit; // this is a RaycastHit obj so that whenever it hits something, things happen
        if(Physics.Raycast(transform.position + rayCastOffset, direction, out hit, raycastDistance)) //Monsters eyes
        {
            if (hit.collider.gameObject.tag == "Player") // Can only hit player to activate chase meaning player can hide behind walls and let the AI walk past.
            {
                // Monster sees player, and starts chasing it, and scary music plays.
                walking = false; 
                investigating = false;
                StopCoroutine("stayIdle");
                StopCoroutine("chaseRoutine");
                StopCoroutine("investigateNoise");
                StartCoroutine("chaseRoutine");
                chasing = true;
                // Scary Music plays.
                if (!scary.isPlaying)
                {
                    scary.Play();
                }
            }
        }
        // if scary monster is chasing, this is set so it keeps going after the player
        if(chasing == true)
        {
            dest = player.position; // sets the new position for monster onto the player object.
            ai.destination = dest; // the ai's destination is now that
            ai.speed = chaseSpeed; // ai moves faster
            aiAnim.ResetTrigger("walk"); //stops other animations to make sure chase animation plays
            aiAnim.ResetTrigger("idle"); // its not idle anymore
            aiAnim.SetTrigger("chase"); // its chasing now!
            #region ATTACKMODE
            if (ai.remainingDistance <= catchDistance) //start of our death
            {
                //player.gameObject.SetActive(false);
                // this is a cooldown where if the monster has hit the player recently, it can't deal damage for 3 seconds. This is done so the player doesn't instantly die on collision, and to be generous. This is temporary of course as it'll still be chasing.
                if(Time.time - recentAtkTime < cooldown)
                {
                    return;
                }
                // if the cooldown is over, then we have the monster attack the player again.
                else
                {
                    player.root.GetComponent<FirstPersonMovement>().takeDamage(); // player takes damage
                    recentAtkTime = Time.time; // we reset the cooldown timer
                }
                // the player
                if (player.root.GetComponent<FirstPersonMovement>().CurrHealth == 0)
                {
                    player.gameObject.SetActive(false);
                    aiAnim.ResetTrigger("walk"); //stopping all other animations to make sure kill animation plays
                    aiAnim.ResetTrigger("idle");
                    aiAnim.ResetTrigger("chase");
                    aiAnim.SetTrigger("kill");
                    StartCoroutine(deathRoutine()); //calls death routine which only happens once (reason why we do not need to check for a stop)
                    chasing = false;
                }
                
            }
            #endregion
        }
        #region INVESTIGATING
        // checks if the monster has been alerted to noise in the area, and it's not chasing or investigating already
        if (dist < PlayerNoiseLvl && !chasing && !investigating)
        {
            investigating = true; // we make sure its investigating now
            lastKnownLoc = player.position; // get the player's position, this is the place where the sound came from.
            //Debug.Log("Investigating"); 
        }
        // if we're investigating, we just do  the walking if statement, but we have it go to the player's last known location.
        if(investigating)
        {
            dest = lastKnownLoc; // sets the new position for monster when it chooses a node.
            ai.destination = dest; // sets the AI's destination to the last known location of the player, and lock it there so the player doesn't keep getting its attention and fucking it over. This does mean its potentially exploitable, but eh.
            // here the monster will investigate any noises that have been made, checking the player's last known position when it was alerted of the noise.
            //Debug.Log("Walking to investigation point");
            ai.speed = walkSpeed; // have the ai be moving at walking speed now
            aiAnim.ResetTrigger("chase"); //reset animations
            aiAnim.ResetTrigger("idle");
            aiAnim.SetTrigger("walk"); //making walk animation play (make sure while in state machine names align)
            if (ai.remainingDistance <= ai.stoppingDistance)
            {
                Debug.Log("Entered if here");
                aiAnim.ResetTrigger("chase"); //stops animations when reaching a node (fail safe to make sure idle animation plays)
                aiAnim.ResetTrigger("walk");
                aiAnim.SetTrigger("idle");
                ai.speed = 0; // have it be idle after its done investigating
                StopCoroutine("stayIdle"); //makes sure that idle is not active before activating idle routine.
                StartCoroutine("stayIdle"); //starts calls our idle routine
                investigating = false; // exit investigation state
            }
        }
        #endregion
        // this checks if we're walking and not investigating, as this is our regular patrol
        #region PATORL
        if (walking == true && !investigating)
        {
            dest = currentDest.position; // sets the new position for monster when it chooses a node.
            ai.destination = dest;
            // here the monster will investigate any noises that have been made, checking the player's last known position when it was alerted of the noise.
            //Debug.Log("I'm walkin' here!");
            ai.speed = walkSpeed;
            aiAnim.ResetTrigger("chase"); //reset animations
            aiAnim.ResetTrigger("idle");
            aiAnim.SetTrigger("walk"); //making walk animation play (make sure while in state machine names align)
            if (ai.remainingDistance <= ai.stoppingDistance)
            {
                Debug.Log("Entered if here");
                aiAnim.ResetTrigger("chase"); //stops animations when reaching a node (fail safe to make sure idle animation plays)
                aiAnim.ResetTrigger("walk");
                aiAnim.SetTrigger("idle");
                ai.speed = 0;
                StopCoroutine("stayIdle"); //makes sure that idle is not active before activating idle routine.
                StartCoroutine("stayIdle"); //starts calls our idle routine
                walking = false;
            }
        }
        #endregion
    }
    #region IDLECOROUTINE
    IEnumerator stayIdle() //Routine call for Idling
    {
        idleTime = (int)Random.Range(minIdleTime, maxIdleTime); // Creates a random idle time (how long monster will stay on node it chose) Can be edited in Unity default values, min: 2. max: 9
        yield return new WaitForSeconds(idleTime); // we wait for whatever random time is decided for its idle animations
        walking = true;
        destinationChoice = Random.Range(0, destinationAmount);
        currentDest = destinations[destinationChoice];

    }
    #endregion
    #region CHASECOROUTINE
    IEnumerator chaseRoutine() //Routine call for Chase
    {
        chaseTime = Random.Range(minChaseTime, maxChaseTime); //Creates a random chase time (how long monster will stay on target) Can be edited in Unity default values, min:10. max:20

        yield return new WaitForSeconds(chaseTime); // Wait between 10 to 20 seconds
        walking = true; // set walking to true
        chasing = false; // set chasing to false
        scary.Stop(); // stop the music
        destinationChoice = Random.Range(0, destinationAmount); // set the destination choice to a random one
        currentDest = destinations[destinationChoice]; // go to that destination
    }
    #endregion
    #region INVESTIGATECOROUTINE
    IEnumerator investigateNoise()
    {
        investigateTime = Random.Range(minInvestigateTime, maxInvestigateTime); // choose a number between 5 and 10
        yield return new WaitForSeconds(investigateTime); // wait between 5-10 seconds
        walking = true; // have it walk the walk
        //Debug.Log("Exitting investigation");
        destinationChoice = Random.Range(0, destinationAmount); // Choose a new destination
        currentDest = destinations[destinationChoice]; // go to that place
    }
    #endregion
    #region DEATHROUTINE
    IEnumerator deathRoutine() //jumpscare. Will call our death scene 
    {
        yield return new WaitForSeconds(jumpscareTime); // wait 2 seconds
        SceneManager.LoadScene(deathScene); // reset the scene after waiting for jumpScareTime, which is 2 seconds
    }
    #endregion
}
