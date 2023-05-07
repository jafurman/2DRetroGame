using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed;
    public float jumpForce; //only need this if you want to control the height of the char jump
    public static bool canMove = true;
    public static bool inDialogueMode = false;
    public Rigidbody2D theRB2D; //IMPORTANT!! need a rigidbody component on the player

    public static bool grounded; //if player == grounded it can jump again
    public bool shoot; //if player == shooting then it can do the shoot animation

    public LayerMask
        whatIsGrd; //layerMask ground is so that we can place in sprites/things in hierarchy that we want to quantify as ground

    public Transform grdChecker;
    public Transform Player;

    public float grdCheckerRad;

    public float airTime;
    public float airTimeCounter; //control height jump

    private bool ctrActive;
    private bool isDead;

    private Collider2D playerCol;
    public GameObject[] childObjs;
    public float shockForce;

    public static bool FacingRight = true;

    private Animator theAnimator;

    public GameManager gm; //If you want to add a title screen and end game screen
    private LivesManager theLM;
    private ScoreManager sm;

    public AudioSource stepSound;
    public AudioSource jumpSound;

    public Animator animator;

    public float meleeRange = 2f;

    private float meleeTimer = 0f;

    private bool allowAttack = true;

    public AudioSource slice;
    public bool hasTeleported = false;
    public bool allowTeleport = true;
    public float teleportTimer = 0f;

    public AudioSource postTSE;
    public AudioSource chargeTSE;

    private bool isKnockedBack = false;

    public static bool isInvincible = false;

    public Transform ScythePoint;
    public float ScytheRange;
    public LayerMask enemyLayers;

    public float ScytheRate = 3f; //can Scythe once per second
    public float nextScytheTime = 0f;

    public AudioSource ScytheSoundOne;
    public AudioSource ScytheSoundTwo;

    public bool hasSwung;
    public int swingCount ;
    public static bool canJump = true;
    


    // Start is called before the first frame update
    private void Start()
    {
        theLM = FindObjectOfType<LivesManager>();
        theRB2D = GetComponent<Rigidbody2D>(); //instance of rigidbody
        theAnimator = GetComponent<Animator>();

        playerCol = GetComponent<Collider2D>();

        airTimeCounter = airTime;

        ctrActive = true;
        swingCount = 0;
    }
    

    // Update is called once per frame
    private void Update()
    {

        if (Time.time >= nextScytheTime)
        {
            if (Input.GetKeyDown(KeyCode.O) && grounded)
            {
                nextScytheTime = Time.time + 1f / ScytheRate;

                if (!hasSwung)
                {
                    hasSwung = true;
                    Scythe();
                }
                else
                {
                    ScytheUp();
                    hasSwung = false; // reset the flag
                }
            }
        }
        
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            inDialogueMode = false;
        }

        //if input is left, can move, if input is right, can move
        if (Input.GetAxisRaw("Horizontal") > 0.5f || Input.GetAxisRaw("Horizontal") < -0.5f )
        {
            if (isKnockedBack)
            {
                canMove = false;
            }
            canMove = true;
        }

        //Stepping noises enabling and disabling
        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && grounded)
        {
            if (Slider.isSliding) //so there isn't noise when the character is sliding on wall
            {
                stepSound.enabled = false;
            }

            stepSound.enabled = true;
        }
        else
        {
            stepSound.enabled = false;

        }

        //upwards and downwards animation.
        if (theRB2D.velocity.y < -0.05f)
        {
            animator.SetBool("Downwards", true);
            animator.SetBool("Grounded", false);

        }
        else if (!grounded)
        {
            animator.SetBool("Downwards", false);
        }

        //Hold M to do the slice feature is implemented here
        if (Input.GetKeyDown(KeyCode.M) && meleeTimer == 0f)
        {
            meleeTimer = 0.01f;
            animator.SetBool("HoldSlice", true);
        }

        if (Input.GetKeyUp(KeyCode.M) && allowAttack)
        {
            meleeTimer = 0;
            animator.SetBool("HoldSlice", false);
        }

        if (meleeTimer > 0)
        {
            meleeTimer += Time.deltaTime;
            if (meleeTimer >= 1.0f && allowAttack)
            {
                PerformMeleeAttack();
                slice.Play();
                allowAttack = false;
            }

            if (meleeTimer >= 1.41f)
            {
                animator.SetBool("HoldSlice", false);
                allowAttack = true;
                meleeTimer = 0f;
            }
        }

        //Teleportation code
        if ((Input.GetKeyDown(KeyCode.N) && !hasTeleported) && teleportTimer == 0f)
        {
            teleportTimer = 0.01f;
            animator.SetBool("Teleport", true);
            chargeTSE.enabled = true;
            //hold down the key, teleport
        }

        if (Input.GetKeyUp(KeyCode.N))
        {
            teleportTimer = 0;
            animator.SetBool("Teleport", false);
            chargeTSE.enabled = false;
            //release the key, can teleport again to avoid 60 teleports per second
            hasTeleported = false;
        }

        if (teleportTimer > 0)
        {
            teleportTimer += Time.deltaTime;
            if (teleportTimer >= 1.1f && allowTeleport && !hasTeleported)
            {
                Teleport();
                //turn off charge SE when teleported to avoid overlap
                chargeTSE.enabled = false;
                postTSE.Play();
                hasTeleported = true;
                allowTeleport = true;
            }

            //however long the teleport animation needs to finish
            if (teleportTimer >= 1.8f)
            {
                animator.SetBool("Teleport", false);
                allowTeleport = true;
                teleportTimer = 0f;
            }
        }
    }


    void Scythe()
    {
        Vector2 playerPosition = gameObject.transform.position;
        Vector2 moveDirection = gameObject.transform.right;
        if (!FacingRight)
        {
            moveDirection *= -1;
        }

        gameObject.transform.Translate(moveDirection * 0.2f);
        //play audio
        ScytheSoundOne.PlayOneShot(ScytheSoundOne.clip);
        animator.SetTrigger("ScytheAttack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(ScythePoint.position, ScytheRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<Enemy>().TakeDamage(1);
            //Enemy.TakeDamage(1);
        }
    }
    
    void ScytheUp()
    {
        //play audio
        ScytheSoundTwo.PlayOneShot(ScytheSoundTwo.clip);
        animator.SetTrigger("ScytheUpAttack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(ScythePoint.position, ScytheRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<Enemy>().TakeDamage(1);
            //Enemy.TakeDamage(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (ScythePoint == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(ScythePoint.position, ScytheRange);
    }

    void Teleport()
    {
        
        Vector2 raycastDirection = transform.right;
        Vector2 raycastStart = (Vector2)transform.position + raycastDirection * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(raycastStart, raycastDirection);

        if (hit.collider != null)
        {
            float distance = hit.distance;

            if (distance > 0)
            {
                if (distance > 3)
                {
                    transform.position = transform.position + (Vector3)(raycastDirection * (3));
                    Debug.Log("The distance is greater than 3: " + distance);
                }
                else
                {
                    transform.position = transform.position + (Vector3)(raycastDirection * (distance + 0.2f));
                    Debug.Log("The distance is less than or equal to 3: " + distance);
                }
            }
            else
            {
                Debug.Log("The distance is 0");
            }
        }
        else
        {
            Debug.Log("The raycast did not hit anything");
        }
    }

    private void PerformMeleeAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, meleeRange);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Spooder"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                Spider spider = hit.GetComponent<Spider>();

                if (enemy != null)
                {
                    enemy.TakeDamage(1);

                }
            }
        }
    }

    private void FixedUpdate()
    {
        grounded = Physics2D.OverlapCircle(grdChecker.position, grdCheckerRad, whatIsGrd); //controlling jump height

        if (ctrActive)
        {
            MovePlayer();
            Jump();
            
        }
    }

    void MovePlayer()
    {
        if (canMove && !inDialogueMode)
        {
            theRB2D.velocity =
                new Vector2(Input.GetAxisRaw("Horizontal") * speed,
                    theRB2D.velocity.y); //Vector 2 is essentially a unity movement plaftorm on a x,y,z plane. 
            //important to mention that "horizontal" keys on getAxisRaw apply to left and right arrows and 'a' and 'd' keys.
            theAnimator.SetFloat("Speed", Mathf.Abs(theRB2D.velocity.x)); //animation code
        }

        if (theRB2D.velocity.x > 0 &&
            !FacingRight) //more sprite animation code so that the character can turn around when going opposing direction 
        {
            // transform.localScale = new Vector2(1f, 1f);
            Flip();
        }
        else if (theRB2D.velocity.x < 0 && FacingRight)
        {
            //transform.localScale = new Vector2(-1f, 1f);
            Flip();
        }
    }

    private void Flip()
    {
        FacingRight = !FacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    void Jump()
    {

        if (grounded && !inDialogueMode && canJump)
        {
            if ((Input.GetKeyDown(KeyCode.W) || Input.GetMouseButtonDown(0)))
            {
                theRB2D.velocity = new Vector2(theRB2D.velocity.x, jumpForce);
                Debug.Log("Calling");
                jumpSound.PlayOneShot(jumpSound.clip);
            }
        }

        if ((Input.GetKey(KeyCode.W) || Input.GetMouseButton(0)) && !inDialogueMode)
        {
            if (airTimeCounter > 0)
            {
                theRB2D.velocity = new Vector2(theRB2D.velocity.x, jumpForce);
                airTimeCounter -= Time.deltaTime;
            }
        }

        if (Input.GetKeyUp(KeyCode.W) || Input.GetMouseButtonUp(0))
        {
            airTimeCounter = 0;
        }

        if (grounded)
        {
            airTimeCounter = airTime;
        }

        theAnimator.SetBool("Grounded", grounded);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((other.gameObject.tag == "Enemy" && !isKnockedBack) || (other.gameObject.tag == "Spooder" && !isKnockedBack))
        {
            if (Mathf.Abs(transform.position.z - other.transform.position.z) > 0.01f)
            {
                // ignore any collisions between the two colliders
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), other);
            }
            StartCoroutine(KnockBack(other.gameObject));
            StartCoroutine(flashSprite());
        }
    }

   

    IEnumerator KnockBack(GameObject enemy)
    {

        isKnockedBack = true;

        Vector2 enemyPosition = enemy.transform.position;
        Vector2 playerPosition = transform.position;
        Vector2 direction = playerPosition - enemyPosition;
        direction = new Vector2(-direction.x, direction.y);

        // Determine the direction the character is facing
        float directionToUse = Mathf.Sign(direction.x);

        // Apply the knockback force in the correct direction
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        //rb.velocity = new Vector2(direction * knockbackDirection.x * 2 , knockbackDirection.y / 3) * 6f;
        rb.velocity = new Vector2(-direction.x * 7f, 1.2f);

        yield return new WaitForSeconds(1f);
        
        isKnockedBack = false;
    }

    public IEnumerator flashSprite()
    {
        isInvincible = true;
        SpriteRenderer sprite = gameObject.GetComponent<SpriteRenderer>();
        sprite.enabled = false;
        yield return new WaitForSeconds(.2f);
        sprite.enabled = true;
        yield return new WaitForSeconds(.2f);
        sprite.enabled = false;
        yield return new WaitForSeconds(.2f);
        sprite.enabled = true;
        yield return new WaitForSeconds(.2f);
        sprite.enabled = false;
        yield return new WaitForSeconds(.2f);
        sprite.enabled = true;
        isInvincible = false;
    }


}