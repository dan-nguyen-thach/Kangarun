﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Controller2D : RaycastController {
	
	float maxClimbAngle = 80;
	float maxDescendAngle = 80;
	
	public CollisionInfo collisions;
	[HideInInspector]
	public Vector2 playerInput;
    public AudioClip coinPick;
    private ScoreManager scoreManager;
    private float downSpeed;
	public override void Start() {
		base.Start ();
		collisions.faceDir = 1;
        scoreManager = FindObjectOfType<ScoreManager>();
    }
	
	public Vector2 Move(Vector2 moveAmount, bool standingOnPlatform) {
		return Move (moveAmount, Vector2.zero, standingOnPlatform);
	}

	public Vector2 Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false) {
		UpdateRaycastOrigins ();

		collisions.Reset ();
		collisions.moveAmountOld = moveAmount;
		playerInput = input;

		if (moveAmount.x != 0) {
			collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
		}

		if (moveAmount.y < 0) {
			DescendSlope(ref moveAmount);
		}

		HorizontalCollisions (ref moveAmount);
		if (moveAmount.y != 0) {
			VerticalCollisions (ref moveAmount);
		}

		transform.Translate (moveAmount);

		if (standingOnPlatform) {
			collisions.below = true;
		}

		return moveAmount;
	}

	void HorizontalCollisions(ref Vector2 moveAmount) {
		float originalMoveAmountX = moveAmount.x;
		Collider2D otherCollider = null;

		float directionX = collisions.faceDir;
		float rayLength = Mathf.Abs (moveAmount.x) + skinWidth;

		if (Mathf.Abs(moveAmount.x) < skinWidth) {
			rayLength = 2*skinWidth;
		}

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            if (hit)
            {

                if (hit.distance == 0)
                {
                    continue;
                }

                otherCollider = hit.collider;

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }


            if (otherCollider != null && otherCollider.gameObject != this.gameObject && otherCollider.tag == "Pushable")
            {
                Vector2 pushAmount = otherCollider.gameObject.GetComponent<PushableObject>().Push(new Vector2(originalMoveAmountX, 0));
                //print (moveAmount.y);
                moveAmount = new Vector2(pushAmount.x, moveAmount.y + pushAmount.y);
                collisions.left = false;
                collisions.right = false;
            }

            if (otherCollider != null && otherCollider.gameObject != this.gameObject && otherCollider.tag == "Death")
            {
                this.gameObject.transform.position = new Vector3(-7.834454f, 3.740344f, 0.078125f);
            }

            if (otherCollider != null && otherCollider.gameObject != this.gameObject && otherCollider.tag == "Coin")
            {

                //otherCollider.gameObject.SetActive(false);
                Destroy(otherCollider.gameObject);      
                scoreManager.AddScore();
                AudioSource.PlayClipAtPoint(coinPick, this.gameObject.transform.position);
                break;
            }
            if (otherCollider != null && otherCollider.gameObject != this.gameObject && otherCollider.tag == "Exit")
            {
                PlayerPrefs.SetInt("scoreCount", scoreManager.scoreCount);
                PlayerPrefs.SetFloat("time", scoreManager.time);
                SceneManager.LoadScene("Scores");
            }
        }
    }
	
	void VerticalCollisions(ref Vector2 moveAmount) {
		float directionY = Mathf.Sign (moveAmount.y);
		float rayLength = Mathf.Abs (moveAmount.y) + skinWidth;
        for (int i = 0; i < verticalRayCount; i ++) {

			Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.up * directionY,Color.red);

			if (hit) {
                if (hit.collider.tag == "Coin")
                {
                    Destroy(hit.collider.gameObject);
                    scoreManager.AddScore();
                    AudioSource.PlayClipAtPoint(coinPick, this.gameObject.transform.position);
                    break;
                }
                if (hit.collider.tag == "Exit")
                {
                    PlayerPrefs.SetInt("scoreCount", scoreManager.scoreCount);
                    PlayerPrefs.SetFloat("time", scoreManager.time);
                    SceneManager.LoadScene("Scores");

                }
                if (hit.collider.tag == "Falling")
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if (collisions.fallingThroughPlatform)
                    {
                        continue;
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", .5f);
                        continue;
                    }
                    downSpeed += Time.deltaTime;
                  
                   Rigidbody2D rb2d = hit.collider.gameObject.GetComponent<Rigidbody2D>();
                    rb2d.isKinematic = false;
                   //hit.collider.gameObject.transform.position = new Vector3(hit.collider.gameObject.transform.position.x, hit.collider.gameObject.transform.position.y-downSpeed, hit.collider.gameObject.transform.position.z);
                   Destroy(hit.collider.gameObject, 2f);
                }

                if (hit.collider.tag == "Through") {
					if (directionY == 1 || hit.distance == 0) {
						continue;
					}
					if (collisions.fallingThroughPlatform) {
						continue;
					}
					if (playerInput.y == -1) {
						collisions.fallingThroughPlatform = true;
						Invoke("ResetFallingThroughPlatform",.5f);
						continue;
					}
				}

                if (hit.collider.tag == "Death")
                {
                    this.gameObject.transform.position = new Vector3(-7.834454f, 3.740344f, 0.078125f);
                }

           

                moveAmount.y = (hit.distance - skinWidth) * directionY;
				rayLength = hit.distance;

				if (collisions.climbingSlope) {
					moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;
			}
		}


        if (collisions.climbingSlope) {
			float directionX = Mathf.Sign(moveAmount.x);
			rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right * directionX,rayLength,collisionMask);

			if (hit) {
				float slopeAngle = Vector2.Angle(hit.normal,Vector2.up);
				if (slopeAngle != collisions.slopeAngle) {
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
				}
			}
		}
       
    }

	void ClimbSlope(ref Vector2 moveAmount, float slopeAngle) {
		float moveDistance = Mathf.Abs (moveAmount.x);
		float climbmoveAmountY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (moveAmount.y <= climbmoveAmountY) {
			moveAmount.y = climbmoveAmountY;
			moveAmount.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (moveAmount.x);
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
		}
	}

	void DescendSlope(ref Vector2 moveAmount) {
		float directionX = Mathf.Sign (moveAmount.x);
		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

		if (hit) {
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
				if (Mathf.Sign(hit.normal.x) == directionX) {
					if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) {
						float moveDistance = Mathf.Abs(moveAmount.x);
						float descendmoveAmountY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
						moveAmount.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (moveAmount.x);
						moveAmount.y -= descendmoveAmountY;

						collisions.slopeAngle = slopeAngle;
						collisions.descendingSlope = true;
						collisions.below = true;
					}
				}
			}
		}
	}

	void ResetFallingThroughPlatform() {
		collisions.fallingThroughPlatform = false;
	}

	public struct CollisionInfo {
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
		public bool descendingSlope;
		public float slopeAngle, slopeAngleOld;
		public Vector2 moveAmountOld;
		public int faceDir;
		public bool fallingThroughPlatform;

		public void Reset() {
			above = below = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;

			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

}
