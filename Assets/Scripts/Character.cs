using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Random = UnityEngine.Random;

public class Character : BaseCharacter
{
    void Start() {
        filename = "QLChar1.dat";
        startConstant = 7f;
        yConstant = 0f;
        targetThreshold = 0.5f;
        learningRate = 0.8f;
        discountFactor = 0.95f;
        explorationRate = 0.5f;

        animator = GetComponent<Animator>();
        animator.SetBool("is_idle", false);
        explorationRate = 0.5f;

        startPosition = new Vector3(startConstant, yConstant, -startConstant);
        target = new Vector3(-startConstant, yConstant, startConstant);
        transform.position = startPosition;
        gridSize = Mathf.RoundToInt((startConstant - (-startConstant)) / stepSize) + 1;
        
        GameObject[] obstacleObjects = GameObject.FindGameObjectsWithTag("Obstacle");

        foreach (GameObject obstacleObject in obstacleObjects) {
            obstacles.Add(obstacleObject.transform);
        }

        qTable = new float[gridSize, gridSize, 4];
        currentState = stateFromPosition(transform.position);
        distanceToTarget = Vector3.Distance(transform.position, target);
        distanceToObstacle = distanceToNearestObstacle(transform.position);
        distanceToPreviousTarget = float.MaxValue;

        currentStep = 0;

        if (QTableManager.loadQTable(filename) != null) {
            qTable = QTableManager.loadQTable(filename);
            isSavedQTable = true;
            maxEpisodes = 0;
            Application.targetFrameRate = singleEpisodeFrameRate;
            print("Using saved values!");
        }
    }

    void Update() {
        if (episodeCount > maxEpisodes) {
            if (!isSavedQTable) {
                QTableManager.saveQTable(qTable, filename);
                ExportCSVData.exportToCSV("qvalue.csv", averageQValues, steps);
            }
            return;
        }
        if (currentStep >= maxSteps || (Mathf.Abs(transform.position.x - target.x) <= targetThreshold && Mathf.Abs(transform.position.z - target.z) <= targetThreshold)) {
            endEpisode();
            return;
        }
        
        if (isSavedQTable) {
            runLearnedTable();
        } else {
            runQLearning();
        }
    }
}
