using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCharacter : MonoBehaviour
{
    protected string filename;
    public Animator animator;
    protected float startConstant;
    protected float yConstant;
    protected float stepSize = 0.3f;
    public Vector3 target;
    public Vector3 startPosition;
    protected float targetThreshold;
    public float learningRate;
    public float discountFactor;
    public float explorationRate;
    public int gridSize;
    protected bool isSavedQTable = false;

    protected float[,,] qTable;
    protected State currentState;
    protected float distanceToTarget;
    protected float distanceToObstacle;
    protected float distanceToPreviousTarget;
    protected List<Transform> obstacles = new List<Transform>();
    protected int maxSteps = 10000000;
    protected int currentStep;
    protected int episodeCount = 0;
    protected int maxEpisodes = 1000;
    protected int singleEpisodeFrameRate = 20;
    protected bool isRotating = false;
    protected List<float> averageQValues = new List<float>() {};
    protected List<int> steps = new List<int>() {};


    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 120;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected struct State {
        public int x;
        public int z;

        public State(int x, int z) {
            this.x = x;
            this.z = z;
        }

        public Vector3 position(int gridSize, float stepSize, float startConstant) {
            float range = startConstant * 2;
            float xPos = ((float)x / (gridSize - 1)) * range - startConstant;
            float zPos = ((float)z / (gridSize - 1)) * range - startConstant;

            return new Vector3(xPos, 0f, zPos);
        }
    }

     protected void runLearnedTable() {
        if (!isRotating) {
            int action = chooseAction();
            move(action);
            State newState = stateFromPosition(transform.position);
            currentState = newState;
            currentStep += 1;
        }
    }

    protected void runQLearning() {
        distanceToTarget = Vector3.Distance(transform.position, target);
        distanceToObstacle = distanceToNearestObstacle(transform.position);

        int action = chooseAction();
        move(action);

        State newState = stateFromPosition(transform.position);
        float rewardVal = reward(newState);
        updateQTable(currentState, action, newState, rewardVal);

        currentState = newState;

        currentStep += 1;
    }

    protected void endEpisode() {
        if (isSavedQTable) {
            animator.SetBool("is_idle", true);
        } else {
            transform.position = startPosition;
            averageQValues.Add(qtableValue());
            steps.Add(currentStep);
        }
        print("Average Q-value: " + qtableValue() + " Episode No: " + episodeCount + " Steps: " + currentStep);
        episodeCount += 1;
        currentStep = 0;
    }

    protected float qtableValue() {
        float sum = 0;
        int count = 0;
        for (int i = 0; i < qTable.GetLength(0); i++)
        {
            for (int j = 0; j < qTable.GetLength(1); j++)
            {
                for (int k = 0; k < qTable.GetLength(2); k++)
                {
                    sum += qTable[i, j, k];
                    count++;
                }
            }
        }
        float average = sum / count;

        return average;
    }

    protected int chooseAction() {
        // Character1: 0 -> forwward, 1 -> backward, 2 -> right, 3 -> left
        // Character2: 0 -> backward, 1 -> forward, 2 -> left, 3 -> right
        List<int> actions = new List<int>() {0, 1, 2, 3};
        if (transform.position.x >= startConstant && transform.position.z >= startConstant) {
            actions.Remove(2);
            actions.Remove(0);
        } else if (transform.position.x <= -startConstant && transform.position.z <= -startConstant) {
            actions.Remove(3);
            actions.Remove(1);
        } else if (transform.position.x >= startConstant && transform.position.z <= -startConstant) {
            actions.Remove(2);
            actions.Remove(1);
        } else if (transform.position.x <= -startConstant && transform.position.z >= startConstant) {
            actions.Remove(3);
            actions.Remove(0);
        } else if (transform.position.x >= startConstant) {
            actions.Remove(2);
        } else if (transform.position.z >= startConstant) {
            actions.Remove(0);
        } else if (transform.position.x <= -startConstant) {
            actions.Remove(3);
        } else if (transform.position.z <= -startConstant) {
            actions.Remove(1);
        }

        if (Random.value < explorationRate && !isSavedQTable) {
            int index = Random.Range(0, actions.Count);
            return actions[index];
        }

        int bestAction = 0;
        float bestQValue = float.MinValue;
        for (int i = 0; i < actions.Count; i++) {
            float qValue = qTable[currentState.x, currentState.z, actions[i]];
            if (qValue > bestQValue) {
                bestQValue = qValue;
                bestAction = actions[i];
            }
        }

        return bestAction;
    }

    protected void move(int action) {
        // Character1: 0 -> forwward, 1 -> backward, 2 -> right, 3 -> left
        // Character2: 0 -> backward, 1 -> forward, 2 -> left, 3 -> right
        Vector3 newPosition = transform.position;
        
        Vector3 direction = transform.forward;
        switch (action) {
            case 0:
                direction = Vector3.forward;
                newPosition.z += stepSize;
                break;
            case 1:
                direction = -Vector3.forward;
                newPosition.z -= stepSize;
                break;
            case 2:
                direction = Vector3.right;
                newPosition.x += stepSize;
                break;
            case 3:
                direction = -Vector3.right;
                newPosition.x -= stepSize;
                break;
        }

        Quaternion newRotation = Quaternion.LookRotation(direction, transform.up);

        if (!isSavedQTable) {
            float clampedX = Mathf.Clamp(newPosition.x, -startConstant, startConstant);
            float clampedZ = Mathf.Clamp(newPosition.z, -startConstant, startConstant);
            newPosition = new Vector3(clampedX, newPosition.y, clampedZ);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, newPosition - transform.position, out hit)) {
                if (hit.transform.tag == "Obstacle" &&  hit.distance <= stepSize) {
                    return;
                }
            }

            transform.rotation = newRotation;
            transform.position = newPosition;
        } else {
            if (transform.rotation != newRotation) {
                StartCoroutine(rotateTowards(newRotation));
            } else {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, newPosition - transform.position, out hit)) {
                    if (hit.transform.tag == "Obstacle" &&  hit.distance <= stepSize) {
                        return;
                    }
                }
                transform.position = newPosition;
            }
        }
    }

    IEnumerator rotateTowards(Quaternion newRotation) {
        isRotating = true;
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0;
        float rotationTime = 0.25f;
        while (elapsedTime < rotationTime) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationTime;
            transform.rotation = Quaternion.Lerp(startRotation, newRotation, t);
            Vector3 newPosition = transform.position + (transform.forward * (stepSize / (singleEpisodeFrameRate * elapsedTime))); 
            float clampedX = Mathf.Clamp(newPosition.x, -startConstant, startConstant);
            float clampedZ = Mathf.Clamp(newPosition.z, -startConstant, startConstant);
            newPosition = new Vector3(clampedX, yConstant, clampedZ);
            RaycastHit hit;
            if (Physics.Raycast(transform.position, (newPosition - transform.position), out hit)) {
                if (hit.transform.tag == "Obstacle") {
                    if (hit.distance <= stepSize) {
                        continue;
                    }
                }
            }
            transform.position = newPosition;
            yield return null;
        }   
        State newState = stateFromPosition(transform.position);
        currentState = newState;
        isRotating = false;
    }

    protected float reward(State newState) {
        float newDistanceToTarget = Vector3.Distance(newState.position(gridSize, stepSize, startConstant), target);
        float distanceReward = (distanceToTarget - newDistanceToTarget) / distanceToTarget;

        float obstaclePenalty = 0f;
        if (distanceToObstacle <= 0.5f) {
            obstaclePenalty = -0.5f;
        }

        return distanceReward + obstaclePenalty;
    }

    protected void updateQTable(State state, int action, State newState, float reward) {
        float oldQValue = qTable[state.x, state.z, action];
        float newQValue = oldQValue + learningRate * (reward + discountFactor * maxQValue(newState) - oldQValue);
        qTable[state.x, state.z, action] = newQValue;
    }

    protected float maxQValue(State state) {
        float maxQValue = float.MinValue;
        for (int i = 0; i < 4; i++) {
            float qValue = qTable[state.x, state.z, i];
            if (qValue > maxQValue) {
                maxQValue = qValue;
            }
        }
        return maxQValue;
    }

    protected State stateFromPosition(Vector3 position) {
        int range = (int)(startConstant * 2);
        int x = (int)(((position.x - (-startConstant)) / range) * (gridSize - 1));
        int z = (int)(((position.z - (-startConstant)) / range) * (gridSize - 1));

        return new State(x, z);
    }

    protected float distanceToNearestObstacle(Vector3 position) {
        float minDistance = float.MaxValue;
        foreach (Transform obstacle in obstacles) {
            float distance = Vector3.Distance(position, obstacle.position);
            if (distance < minDistance) {
                minDistance = distance;
            }
        }
        return minDistance;
    }
}
