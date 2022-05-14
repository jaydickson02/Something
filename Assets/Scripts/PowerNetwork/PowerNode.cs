using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerNode : MonoBehaviour
{
    [SerializeField]
    private String deviceType; //generator, consumer, transmitter

    [SerializeField]
    private Material working;

    [SerializeField]
    private Material failed;

    public int distributionDistance = 10;

    public int PowerConsumption = 0;

    public int PowerProduction = 0;

    public List<GameObject> directlyConnectedDevices;

    public List<GameObject> networkDevicesConnected; //All devices connected in a valid way to this node. Does not include this node itself

    public bool hasPower = false;

    // Update is called once per frame
    void Update()
    {
        //Reset Connected Devices
        directlyConnectedDevices.Clear();

        //Assign valid and connected nodes to directlyConnectedDevices
        setConnectedNodes();

        //Consummer Specific Functions
        if (deviceType == "consumer")
        {
            constructNetwork();
            int netPower = evaluateNetPower();
            updatePowerStatus(netPower);
            updateTexture();
            Debug.Log("NetPower: " + netPower);
        }

        //Debugging
        if (deviceType == "consumer")
        {
            Debug.Log("Direct: " + directlyConnectedDevices.Count);
            Debug.Log("Network: " + networkDevicesConnected.Count);

        }

    }

    private void updatePowerStatus(int netPower)
    {
        if (netPower >= 0)
        {
            hasPower = true;
        }
        else
        {
            hasPower = false;
        }
    }

    private int evaluateNetPower()
    {
        int netPower = 0;
        int powerProduced = 0;
        int powerConsumed = 0;

        foreach (GameObject node in networkDevicesConnected)
        {
            PowerNode nodeScript = node.GetComponent<PowerNode>(); //Get refrence to the power nodes script for the particular object

            powerConsumed += nodeScript.PowerConsumption;
            powerProduced += nodeScript.PowerProduction;
        }

        //Include this power object
        powerConsumed += PowerConsumption;
        powerProduced += PowerProduction;

        netPower = powerProduced - powerConsumed;

        return netPower;
    }


    //This function is recursive. Gives all the connected devices as a list.
    public List<GameObject> evaluateConnectedDevices(List<GameObject> dontCheck)
    {
        List<GameObject> dontCheckUpdated = new List<GameObject>();//Create a new dont check list 
        dontCheckUpdated.AddRange(dontCheck); //Pass the orignial dont check list.


        List<GameObject> connections = new List<GameObject>(); //Initialise a blank list for tracking valid connections.
        /*Check for new connections that haven't been previously evaluated and add them to the valid connections 
        list also update the dont check list to reflect that these have been registered.
        */

        foreach (GameObject connection in directlyConnectedDevices)
        {
            if (!isInList(dontCheck, connection)) //Check if the connection is in the dontCheck list
            {
                dontCheckUpdated.Add(connection); //Update the dontcheck list to be passed to the next recursive run.
                connections.Add(connection); //Add as a valid connection
            }
        }

        /*Check that connections are not in the dontCheck list and if they are valid run this same function on 
        them with the updated dontcheck list. Add the result to the valid connections list.
        */

        foreach (GameObject connection in directlyConnectedDevices)
        {
            if (!isInList(dontCheck, connection))
            {
                PowerNode connectionScript = connection.GetComponent<PowerNode>(); //Create a refrence to the connections power script
                List<GameObject> newConnections = connectionScript.evaluateConnectedDevices(dontCheckUpdated);
                connections.AddRange(newConnections); //Run this function with the updated dontcheck list and add to valid connections.

                /*This prevents recognition of multiple nodes if multiple transmission nodes are 
                bridging generators and consumers but the consumers and generators are connected to all the same transmission nodes. 
                
                For example: gen1 is connected to 2 transmission nodes and consumer1 is also connected to the same 2 transmission nodes. 
                In this case dontCheck does not account for the extra instance of the generator in the trasmitters directlyConnectedDevices array*/
                if (newConnections.Count > 0)
                {
                    foreach (GameObject newConnection in newConnections)
                    {
                        if (!isInList(dontCheckUpdated, newConnection))
                        {
                            dontCheckUpdated.Add(newConnection);
                        }
                    }
                }

            }
        }


        return (connections); //Return the valid connections.

    }

    private void constructNetwork()
    {
        networkDevicesConnected.Clear();
        List<GameObject> dontCheck = new List<GameObject>(); //Create the inital dont check list
        dontCheck.Add(gameObject); //Add this powerNode to it.

        networkDevicesConnected = evaluateConnectedDevices(dontCheck); //Assigns all valid nodes to tracker variable
    }

    private void setConnectedNodes()
    {
        if (deviceType == "consumer")
        {
            connectToValidNodes("transmitter");

        }

        if (deviceType == "transmitter")
        {
            connectToValidNodes("generator");

            connectToValidNodes("transmitter");

            connectToValidNodes("consumer");
        }

        if (deviceType == "generator")
        {
            connectToValidNodes("transmitter");
        }


    }

    private void updateTexture()
    {
        if (hasPower == true) //Change so this only runs on a change in hasPower
        {
            gameObject.GetComponent<MeshRenderer>().material = working;
        }
        else
        {
            gameObject.GetComponent<MeshRenderer>().material = failed;
        }
    }

    private void connectToValidNodes(string nodeType)
    {
        GameObject[] powerNodes = GameObject.FindGameObjectsWithTag("PowerNode"); //Update this to get value from powerManager at some point, this actually needs to check for power nodes

        foreach (GameObject powerNode in powerNodes) //Iterate through all placed power nodes 
        {
            if (powerNode.transform.position != gameObject.transform.position)
            {
                PowerNode powerNodeProperties = powerNode.GetComponent<PowerNode>(); //Assign the powernode script to a variable to access its properties.
                if (powerNodeProperties.deviceType == nodeType)
                {
                    float distance = checkProximity(powerNode, gameObject); //Check the distance between a powernode and the powernode this script is attached to

                    if (distance < powerNodeProperties.distributionDistance) //Check if this powernode is within the distribution distance of another powernode.
                    {
                        directlyConnectedDevices.Add(powerNode);

                    }
                }
            }
        }
    }

    //Utility Functions

    private float checkProximity(GameObject object1, GameObject object2)
    {
        float distance = Vector3.Distance(object1.transform.position, object2.transform.position);

        return distance;
    }

    private bool isInList(List<GameObject> listToBeSearched, GameObject searchValue) //rewrite to be a binary search if too slow.
    {
        foreach (GameObject item in listToBeSearched)
        {
            if (item == searchValue)
            {
                return true;
            }
        }
        return false;
    }

}
