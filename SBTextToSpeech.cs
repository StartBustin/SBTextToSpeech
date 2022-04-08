using MelonLoader;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace SBTextToSpeech
{


    public static class BuildInfo
    {
        public const string Name = "SBTextToSpeech"; // Name of the Mod.  (MUST BE SET)
        public const string Description = "Sends messages to the Text to Speech server that should be running alongside this."; // Description for the Mod.  (Set as null if none)
        public const string Author = "Catssandra Ann M."; // Author of the Mod.  (MUST BE SET)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class SBTextToSpeech : MelonMod
    {
        public static HashSet<string> labLevels = new HashSet<string>();
        public static HashSet<string> unionLevels = new HashSet<string>();
        public static HashSet<string> petrovLevels = new HashSet<string>();
        public static HashSet<string> valkyrieBayLevels = new HashSet<string>();

        public static float timeUntilNextLevelTimerFetch = 0;
        public const float resetTimeUntilNextLevelTimerFetch = 5;
        public static bool showBytesSent = false;

        public static LevelTimer levelTimer;

        public static bool isConnected = true;
        public const string EOF = "<EOF>";
        public const string NEWLINE = "\r\n";

        // These need to be static so that when we get an instance of this object, we know these values will be reliable.
        public static int currentSceneIndex = -1;
        public static string currentSceneName = "";
        public static string previousSceneName = "";

        public static float igt = -1;
        public static Dictionary<string, float> igtByLevel = new Dictionary<string, float>();
        public static bool initGameTimeCalled = false;
        public static bool useGameTime = true;
        public static bool verboseSceneChange = false;

        public static HashSet<string> DoNotTrackSceneTime = new HashSet<string>();

        //private static readonly HttpClient httpClient = new HttpClient();

        //This is kinda useless, but needs to perform unique behavior to make for a good ArrayOfByte scan to find the class definition, and it must actually be called or the compiler will optimize it out.
        public override void OnApplicationStart() // Runs after Game Initialization.
        {
            MelonLogger.Msg("OnApplicationStart - SBTextToSpeech");

            Thread threadNetworking = new Thread(AsynchronousClient.StartClient);
            threadNetworking.Start();
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName) // Runs when a Scene has Loaded and is passed the Scene's Build Index and Name.
        {
            //httpClient.GetAsync("http://localhost:8743/?speak=" + uriEncodedSceneName);

            isConnected = (AsynchronousClient.client != null);
            if (isConnected)
            {
                MelonLogger.Msg("Attempting to send HTTP request.");
                string uriEncodedSceneName = System.Uri.EscapeDataString(sceneName);
                AsynchronousClient.SendHttpGetMessage(uriEncodedSceneName);
                MelonLogger.Msg("HTTP Request should have been sent...");
            }
            else
            {
                MelonLogger.Msg("Attempted to send a Socket Message but was not connected.");
            }
        }

        public override void OnUpdate()
        {
            
        }

        private void AttemptReconnect()
        {
            AsynchronousClient.StartClient();
        }

        public void SendSocketSceneInfo(int buildindex, string sceneName)
        {
            SendSocketMessage(buildindex.ToString() + ":" + sceneName);
        }

        public void SendSocketMessage(string msg)
        {
            try
            {
                AsynchronousClient.Send(AsynchronousClient.client, msg + NEWLINE);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                //MelonLogger.Msg("Warning: AutoSplitter appears to be disonnected from the server.");
                AsynchronousClient.client = null;
            }
        }

        public void SendSocketMessage(string msg, bool logSend)
        {
            AsynchronousClient.Send(AsynchronousClient.client, msg + NEWLINE);
            if (logSend) { MelonLogger.Msg("SENT: " + msg + NEWLINE); }
        }

        public override void OnApplicationQuit() // Runs when the Game is told to Close.
        {
            MelonLogger.Msg("OnApplicationQuit");
            AsynchronousClient.StopClient();
        }

        public float GetTime()
        {
            RefreshLevelTimerReference();

            if (levelTimer != null)
            {
                UpdateIGT(currentSceneName, SBCalcTimeFromTimerDigits());
            }

            return SumIGT();
        }

        private static void RefreshLevelTimerReference()
        {
            timeUntilNextLevelTimerFetch -= UnityEngine.Time.deltaTime;
            if (timeUntilNextLevelTimerFetch <= 0)
            {
                timeUntilNextLevelTimerFetch += resetTimeUntilNextLevelTimerFetch;

                levelTimer = UnityEngine.GameObject.FindObjectOfType<LevelTimer>();
                if (verboseSceneChange) { LogSumIGT(); }
            }
        }

        public float SumIGT()
        {
            float time = 0;
            foreach (float t in igtByLevel.Values)
            {
                time += t;
            }
            return time;
        }

        public static void ResetSumIGT()
        {
            igtByLevel.Clear();
        }

        public static void UpdateIGT(string sceneName, float igt)
        {
            if (!DoNotTrackSceneTime.Contains(sceneName))
            {
                sceneName = AlphaOrCassie() + ":" + GetLevelNameForScene(sceneName);
                if (igtByLevel.ContainsKey(sceneName))
                {
                    igtByLevel[sceneName] = igt;
                }
                else
                {
                    igtByLevel.Add(sceneName, igt);
                }
            }
        }

        public static string GetLevelNameForScene(string sceneName)
        {
            string levelName = "Unknown";

            if (labLevels.Contains(sceneName))
            {
                levelName = "labLevels";
            }
            if (unionLevels.Contains(sceneName))
            {
                levelName = "unionLevels";
            }
            if (petrovLevels.Contains(sceneName))
            {
                levelName = "petrovLevels";
            }
            if (valkyrieBayLevels.Contains(sceneName))
            {
                levelName = "valkyrieBayLevels";
            }

            return levelName;
        }

        public static string AlphaOrCassie() 
        {
            string charName = "Alpha";
            if (LevelManager.currentLevel != null && LevelManager.currentLevel.character > 0) 
            {
                charName = "Cassie";
            }
            return charName;
        }

        public static void LogSumIGT()
        {
            MelonLogger.Msg("LogSumIGT: ");
            foreach (string key in igtByLevel.Keys)
            {
                MelonLogger.Msg(key + ": " + TimeSpan.FromSeconds(igtByLevel[key]).ToString(@"d\.hh\:mm\:ss"));
            }
        }

        public static float SBCalcTimeFromTimerDigits()
        {
            float seconds = 0;
            if (levelTimer != null)
            {
                seconds += levelTimer.minutes2 * 600;
                seconds += levelTimer.minutes * 60;
                seconds += levelTimer.seconds2 * 10;
                seconds += levelTimer.seconds;
                seconds += levelTimer.mSeconds;
            }

            return seconds;
        }

        public static float FP2SVCalcTimeFromTimerDigits()
        {
            float seconds = 0;
            if (levelTimer != null)
            {
                seconds += levelTimer.minutes2 * 600;
                seconds += levelTimer.minutes * 60;
                seconds += levelTimer.seconds2 * 10;
                seconds += levelTimer.seconds;
                seconds += levelTimer.mSeconds;
            }

            return seconds;
        }
    }

    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousClient
    {
        public const string EOF = "<EOF>";
        public const string NEWLINE = "\r\n";
        public static Socket client = null;

        public static bool quit = false;
        // The port number for the remote device.  
        //private const int port = 11920;
        /*
        Port number is based on the Starbuster Discord Server creation date: 
        function getCreationDate(id) { return new Date((id / 4194304) + 1420070400000); } getCreationDate("668578578841075712");
         Sun Jan 19 2020 16:12:34 GMT-0600 (Central Standard Time)
         */

        private const int port = 8734; // Livesplit Server default.
        // https://github.com/LiveSplit/LiveSplit.Server

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        public static void StartClient()
        {
            while (!quit)
            {
                // Connect to a remote device.  
                try
                {
                    // Establish the remote endpoint for the socket.  
                    // The name of the
                    // remote device is "host.contoso.com".  
                    //IPHostEntry ipHostInfo = Dns.GetHostEntry("host.contoso.com");
                    //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    //IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPAddress ipAddress = IPAddress.Loopback;
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    // Create a TCP/IP socket.  
                    client = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                    MelonLogger.Msg("TCP/IP Socket created. Attempting to connect.");
                    // Connect to the remote endpoint.  
                    client.BeginConnect(remoteEP,
                        new AsyncCallback(ConnectCallback), client);
                    connectDone.WaitOne();

                    /*
                    // Send test data to the remote device.  
                    Send(client, "This is a test" + NEWLINE);
                    sendDone.WaitOne();

                    // Receive the response from the remote device.  
                    Receive(client);
                    receiveDone.WaitOne();
                    */

                    Receive(client);
                    receiveDone.WaitOne();

                    // Write the response to the console.  
                    MelonLogger.Msg("Response received : {0}", response);

                    if (response.Equals("0\r\n"))
                    {
                        MelonLogger.Msg("Split index appears to be 0. Resetting IGT.");
                        SBTextToSpeech.ResetSumIGT();
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Msg(e.ToString());
                }

            }
        }

        public static void StopClient()
        {
            quit = true;
            receiveDone.Set();

            try
            {
                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                MelonLogger.Msg("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e.ToString());
            }
        }

        public static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);

            //MelonLogger.Msg("Sent: " + data);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                if (SBTextToSpeech.showBytesSent)
                {
                    int bytesSent = client.EndSend(ar);
                    MelonLogger.Msg("Sent {0} bytes to server.", bytesSent);
                }


                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e.ToString());
                AsynchronousClient.client = null;
            }
        }

        public static void SendHttpGetMessage(string msg) 
        {
            
            string httpContentTemplate = @"GET /?speak={REPLACEME} HTTP/1.1
Host: localhost:8734

";

            msg = httpContentTemplate.Replace("{REPLACEME}", System.Uri.EscapeDataString("Oh hey, it'd be kinda neat if you had a cool message here!"));
            MelonLogger.Msg(msg.ToString());
            //msg = msg.Replace("{REPLACEME}", msg);
            Send(client, msg);
        }
    }
}

/*
     // AOB signature for Gametime. Use the address at 8B
	vars.sigGametime = new SigScanTarget(0,"8B 4E 68 C6 45 D3 01");

    var scanner = new SignatureScanner(game, baseAddr, size);
	
	// Scan memory for Gametime signature
	IntPtr staticGametime =	vars.ptrGametime = scanner.Scan((SigScanTarget) vars.sigGametime);
	// Scan memory for Mapload signature
	IntPtr staticMapload  =	vars.ptrMapload  = scanner.Scan((SigScanTarget) vars.sigMapload);
	// Scan memory for Menuload signature
	IntPtr staticMenuload =	vars.ptrMenuload = scanner.Scan((SigScanTarget) vars.sigMenuload);
	
	// Allocate memory for our code instead of looking for a code cave
	vars.log("DEBUG","Allocating memory...");
	var aslMem = vars.aslMem = game.AllocateMemory(64);
	
	var addrMem = BitConverter.GetBytes((int) aslMem).Reverse().ToArray();
	vars.log("DEBUG","aslMem address: "+BitConverter.ToString(addrMem).Replace("-",""));
	
	if(staticGametime == IntPtr.Zero || staticMapload == IntPtr.Zero || staticMenuload == IntPtr.Zero){
		vars.log("ERROR","Can't find signatures. Unknown game version?");
		game.FreeMemory((IntPtr) aslMem);
		MessageBox.Show(
			"Error: Can't find signatures.\n"+
			"\n staticGametime = "+staticGametime+
			"\n staticMapload = "+staticMapload+
			"\n staticMenuload = "+staticMenuload+
			"\n\nTry restarting the game.",
			vars.aslName+" | LiveSplit",
			MessageBoxButtons.OK,MessageBoxIcon.Error
		);
	}	
    https://raw.githubusercontent.com/PrototypeAlpha/AmnesiaASL/master/AmnesiaTDD.asl



The number at the end is the offset as bytes (which means, double it) to treat as the starting position for any offsets you use.

A pair of examples: 

From the PDF: private static ProgramPointer globalGameManager = new ProgramPointer(new FindPointerSignature(PointerVersion.Steam, AutoDeref.Double, "558BEC5783EC34C745E4000000008B4508C74034000000008B05????????83EC086A0050E8????????83C41085C0743A8B05", 50));
In this case, we resolve right the address we find right at the END of this AoB, since this is 25 bytes long. 

CODE:  558BEC5783EC34C745E4000000008B4508C74034000000008B05????????83EC086A0050E8????????83C41085C0743A8B05
COUNT: 1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890^  [char 100, byte 50]

From the example code: private static ProgramPointer platformManager = new ProgramPointer(new FindPointerSignature(PointerVersion.Steam, AutoDeref.Double, "83EC0C50E8????????83C41085C0740B8B05????????E9????????0FB605", 18));

CODE:  83EC0C50E8????????83C41085C0740B8B05????????E9????????0FB605
COUNT: 123456789012345678901234567890123456^  [char 36, byte 18]

 */


/*
 Here's the list of commands:

starttimer
startorsplit
split
unsplit
skipsplit
pause
resume
reset
initgametime
setgametime TIME
setloadingtimes TIME
pausegametime
unpausegametime
setcomparison COMPARISON
The following commands respond with a time:

getdelta
getdelta COMPARISON
getlastsplittime
getcomparisonsplittime
getcurrenttime
getfinaltime
getfinaltime COMPARISON
getpredictedtime COMPARISON
getbestpossibletime
Other commands:

getsplitindex
getcurrentsplitname
getprevioussplitname
getcurrenttimerphase
 */

/*
 SAGE 2021 Demo Scene List
(Cassie)
0 | OpeningCutscene
19 | MainMenuBase
24 | SaveFileMenu
1 | PlayerSelect

6 | Union Tutorial
7 | Union Station
10 | Union Station 2 Rework
11 | Union Station 3 rework

16 | PostUnionHab

15 | Petrov

8 | Valkyrie Bay 1
9 | Valkyrie Bay 2

21 | PostVB
22 | PostVB2
26 | ImperialCutscene
27 | PrimeIntro

(Alpha)

19 | MainMenuBase
24 | SaveFileMenu
1 | PlayerSelect

20 | PreLab19
5 | Tutorial2
2 | Lab19Rework1
3 | Lab19Rework2
4 | Lab19Boss

12 | PostLab19
13 | PetrovIntro
14 | Act1Intro

15 | Petrov

8 | Valkyrie Bay 1
9 | Valkyrie Bay 2

21 | PostVB
22 | PostVB2
26 | ImperialCutscene
27 | PrimeIntro
19 | MainMenuBase

 */