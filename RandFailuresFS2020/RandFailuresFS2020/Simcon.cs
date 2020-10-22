﻿using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RandFailuresFS2020
{
    interface ISimCon
    {
        string Connect();
        string Disconnect();
        void UpdateData();
        void prepareFailures();
        SimConnect GetSimConnect();
        List<SimVar> getFailList();
        void setMaxAlt(int a);
        void setMaxTime(int t);
        void setMaxNoFails(int f);
        void stopTimers();
    }

    enum GROUP
    {
        ID_PRIORITY_STANDARD = 1900000000,
    };

    public enum DEFINITION
    {
        Dummy = 0
    };

    public enum REQUEST
    {
        Dummy = 0
    };

    public enum EVENT
    {
        Dummy = 0
    };

    public enum WHEN_FAIL
    {
        ALT, TIME, TAXI, INSTANT
    };
    public enum POSSIBLE_FAIL_TYPE
    {
        NO, CONTINOUS, COMPLETE, BOTH, STUCK, LEAK
    };

    public class SimVar
    {
        public DEFINITION eDef = DEFINITION.Dummy;
        public REQUEST eRequest = REQUEST.Dummy;
        public EVENT eEvent = EVENT.Dummy;
        public bool isEvent = false;
        public string sName;
        public double dValue;
        public string sUnits;

        public string controlName;

        public int failureChance;
        public POSSIBLE_FAIL_TYPE possibleType;
        public double failureValue;
        public WHEN_FAIL whenFail;
        public int failureHeight;
        public int failureTime;
        public bool done = false;
        public bool failStart = false;

        private Random rnd;


        public SimVar(int id, string _name, string _cname, POSSIBLE_FAIL_TYPE ptype, string _unit = "Number")
        {
            //MessageBox.Show(id.ToString());
            eDef = (DEFINITION)id;
            eRequest = (REQUEST)id;
            isEvent = false;
            sName = _name;
            sUnits = _unit;
            controlName = _cname;
            possibleType = ptype;
        }

        public SimVar(string _name, string _cname, POSSIBLE_FAIL_TYPE ptype = POSSIBLE_FAIL_TYPE.COMPLETE)
        {
            eEvent = EVENT.Dummy;
            possibleType = ptype;
            isEvent = true;
            sName = _name;
            controlName = _cname;
            rnd = new Random();
        }

        public void setFail(SimConnect sc)
        {
            Console.WriteLine(sName + failureValue);
            switch (possibleType)
            {
                case POSSIBLE_FAIL_TYPE.STUCK:
                    {
                        if (!failStart)
                        {
                            failureValue = dValue;
                            Console.WriteLine("start" + failureValue);
                            failStart = true;
                        }
                        dValue = failureValue;
                        break;
                    }
                case POSSIBLE_FAIL_TYPE.LEAK:
                    {
                        dValue -= failureValue;
                        break;
                    }
                case POSSIBLE_FAIL_TYPE.CONTINOUS:
                    {
                        if (isEvent)
                        {
                            if (rnd.Next(11) >= 5)
                            {
                                sc.MapClientEventToSimEvent(eEvent, sName);
                                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, eEvent, 0, GROUP.ID_PRIORITY_STANDARD, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            }
                        }
                        else
                            dValue = failureValue;
                        break;
                    }
                case POSSIBLE_FAIL_TYPE.COMPLETE:
                    {
                        if (isEvent)
                        {
                            sc.MapClientEventToSimEvent(eEvent, sName);
                            sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, eEvent, 0, GROUP.ID_PRIORITY_STANDARD, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        }
                        else
                            dValue = failureValue;

                        done = true;
                        break;
                    }
            }

            sc.SetDataOnSimObject(eDef, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, dValue);
        }
    };


    class Simcon : ISimCon
    {
        public const int WM_USER_SIMCONNECT = 0x0402;
        private IntPtr m_hWnd = new IntPtr(0);
        private SimConnect simconnect = null;
        private DispatcherTimer GroundTimer;
        private DispatcherTimer ContinTimer;
        public List<SimVar> lSimVars;
        public List<SimVar> lWillFailSV;
        public Form1 f1;
        public IEnumerable<Control> controls;

        public int maxAlt = 20000;
        public int maxTime = 3600;
        public int maxNoFails = 99;
        public uint flyTime = 0;

        #region main
        public Simcon(Form1 form)
        {
            GroundTimer = new DispatcherTimer();
            GroundTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            GroundTimer.Tick += new EventHandler(OnTickGround);

            ContinTimer = new DispatcherTimer();
            ContinTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            ContinTimer.Tick += new EventHandler(OnTickContin);

            m_hWnd = form.Handle;

            lSimVars = new List<SimVar>();
            lWillFailSV = new List<SimVar>();

            f1 = form;
        }

        public string Connect()
        {
            Console.WriteLine("Connect");

            try
            {
                /// The constructor is similar to SimConnect_Open in the native API
                simconnect = new SimConnect("RandFailuresFS2020", m_hWnd, WM_USER_SIMCONNECT, null, 0);

                /// Listen to connect and quit msgs
                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);

                /// Listen to exceptions
                simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);

                controls = GetAll(f1, typeof(NumericUpDown));

                InitData();

                /// Catch a simobject data request
                simconnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_OnRecvSimobjectDataBytype);

                UpdateData();
            }
            catch (COMException ex)
            {
                Console.WriteLine("Connection to KH failed: " + ex.Message);
                return "Failed: " + ex.Message;
            }

            return "SimConnect connected";
        }

        void InitData()
        {
            createRegister(new SimVar(lSimVars.Count(), "PLANE ALTITUDE", "", POSSIBLE_FAIL_TYPE.NO, "Feet"));
            createRegister(new SimVar(lSimVars.Count(), "GROUND VELOCITY", "", POSSIBLE_FAIL_TYPE.NO, "Knots"));
            createRegister(new SimVar(lSimVars.Count(), "SIM ON GROUND", "", POSSIBLE_FAIL_TYPE.NO));

            createRegister(new SimVar(lSimVars.Count(), "TRAILING EDGE FLAPS LEFT PERCENT", "fLeftFlap", POSSIBLE_FAIL_TYPE.STUCK));
            createRegister(new SimVar(lSimVars.Count(), "TRAILING EDGE FLAPS RIGHT PERCENT", "fRightFlap", POSSIBLE_FAIL_TYPE.STUCK));

            createRegister(new SimVar(lSimVars.Count(), "GEAR CENTER POSITION", "fCenterGear", POSSIBLE_FAIL_TYPE.STUCK));
            createRegister(new SimVar(lSimVars.Count(), "GEAR LEFT POSITION", "fLeftGear", POSSIBLE_FAIL_TYPE.STUCK));
            createRegister(new SimVar(lSimVars.Count(), "GEAR RIGHT POSITION", "fRightGear", POSSIBLE_FAIL_TYPE.STUCK));

            createRegister(new SimVar(lSimVars.Count(), "FUEL TANK CENTER LEVEL", "fFuelCenter", POSSIBLE_FAIL_TYPE.LEAK));
            createRegister(new SimVar(lSimVars.Count(), "FUEL TANK LEFT MAIN LEVEL", "fFuelLeft", POSSIBLE_FAIL_TYPE.LEAK));
            createRegister(new SimVar(lSimVars.Count(), "FUEL TANK RIGHT MAIN LEVEL", "fFuelRight", POSSIBLE_FAIL_TYPE.LEAK));

            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL AIRSPEED", "fPAirspeed", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL ALTIMETER", "fPAltimeter", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL ATTITUDE", "fPAttitide", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL COMPASS", "fPCompass", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL ELECTRICAL", "fPElectrical", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL AVIONICS", "fPAvionics", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL ENGINE", "fPEngine", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL FUEL INDICATOR", "fPFuelI", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL HEADING", "fPHeading", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL VERTICAL VELOCITY", "fPVS", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL PITOT", "fPPitot", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL TURN COORDINATOR", "fPTurnC", POSSIBLE_FAIL_TYPE.COMPLETE));
            createRegister(new SimVar(lSimVars.Count(), "PARTIAL PANEL VACUUM", "fPVacuum", POSSIBLE_FAIL_TYPE.COMPLETE));

            for (int i = 0; i < 4; i++)
            {
                createRegister(new SimVar(lSimVars.Count(), "ENG ON FIRE:" + (i + 1), "fE" + (i + 1) + "Fire", POSSIBLE_FAIL_TYPE.COMPLETE));
                createRegister(new SimVar(lSimVars.Count(), "RECIP ENG COOLANT RESERVOIR PERCENT:" + (i + 1), "fE" + (i + 1) + "Leak", POSSIBLE_FAIL_TYPE.LEAK));
                createRegister(new SimVar(lSimVars.Count(), "RECIP ENG TURBOCHARGER FAILED:" + (i + 1), "fE" + (i + 1) + "Turbo", POSSIBLE_FAIL_TYPE.COMPLETE));
            }

            createRegister(new SimVar("TOGGLE_VACUUM_FAILURE", "feVacuum"));
            createRegister(new SimVar("TOGGLE_ELECTRICAL_FAILURE", "feElectricalComplete"));
            createRegister(new SimVar("TOGGLE_ELECTRICAL_FAILURE", "feElectricalShort", POSSIBLE_FAIL_TYPE.CONTINOUS));
            createRegister(new SimVar("TOGGLE_PITOT_BLOCKAGE", "fePitot"));
            createRegister(new SimVar("TOGGLE_STATIC_PORT_BLOCKAGE", "feStatic"));
            createRegister(new SimVar("TOGGLE_HYDRAULIC_FAILURE", "feHydraulic"));
            createRegister(new SimVar("TOGGLE_TOTAL_BRAKE_FAILURE", "feTotalBrake"));
            createRegister(new SimVar("TOGGLE_LEFT_BRAKE_FAILURE", "feLeftBrake"));
            createRegister(new SimVar("TOGGLE_RIGHT_BRAKE_FAILURE", "feRightBrake"));
            createRegister(new SimVar("TOGGLE_ENGINE1_FAILURE", "feE1"));
            createRegister(new SimVar("TOGGLE_ENGINE2_FAILURE", "feE2"));
            createRegister(new SimVar("TOGGLE_ENGINE3_FAILURE", "feE3"));
            createRegister(new SimVar("TOGGLE_ENGINE4_FAILURE", "feE4"));
        }

        void createRegister(SimVar temp)
        {
            if (!temp.isEvent)
            {
                simconnect.AddToDataDefinition(temp.eDef, temp.sName, temp.sUnits, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<double>(temp.eDef);
            }

            lSimVars.Add(temp);
        }

        public void prepareFailures()
        {
            GroundTimer.Stop();
            ContinTimer.Stop();
            lWillFailSV.Clear();
            flyTime = 0;

            Random rnd = new Random();
            foreach (SimVar s in lSimVars)
            {
                if (maxNoFails <= 0)
                {
                    break;
                }
                maxNoFails--;
                if (s.controlName != "")
                {
                    try
                    {
                        foreach (Control c in controls)
                        {
                            if (c is NumericUpDown)
                            {
                                if (c.Name == s.controlName)
                                {
                                    s.failureChance = (int)(((NumericUpDown)c).Value);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                if (s.possibleType != POSSIBLE_FAIL_TYPE.NO && rnd.Next(100) < s.failureChance)
                {
                    switch (s.possibleType)
                    {
                        case POSSIBLE_FAIL_TYPE.COMPLETE:
                            {
                                s.failureValue = 1;
                                break;
                            }
                        case POSSIBLE_FAIL_TYPE.LEAK:
                            {
                                s.failureValue = 0.000001 + (rnd.Next(0, 80) / 10000000);
                                break;
                            }
                        case POSSIBLE_FAIL_TYPE.STUCK:
                            {
                                s.failureValue = s.dValue;
                                break;
                            }
                    }

                    s.whenFail = (WHEN_FAIL)rnd.Next(0, 4);

                    switch (s.whenFail)
                    {
                        case WHEN_FAIL.ALT:
                            {
                                s.failureHeight = (int)(lSimVars[0].dValue + 2000 + (rnd.Next(maxAlt)));
                                break;
                            }
                        case WHEN_FAIL.TIME:
                            {
                                s.failureTime = 30 + rnd.Next(maxTime);
                                break;
                            }
                    }

                    lWillFailSV.Add(s);
                }
            }

            GroundTimer.Start();
        }

        public void UpdateData()
        {
            foreach (SimVar s in lSimVars)
            {
                if (!s.isEvent)
                {
                    simconnect.RequestDataOnSimObjectType(s.eRequest, s.eDef, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                }
            }
        }

        private void OnTickGround(object sender, EventArgs e)
        {
            UpdateData();

            if (lSimVars[2].dValue == 0)
                flyTime++;

            if (!ContinTimer.IsEnabled)
            {
                ContinTimer.Start();
            }
        }

        private void OnTickContin(object sender, EventArgs e)
        {
            foreach (SimVar s in lWillFailSV)
            {
                if (!s.done)
                {
                    switch (s.whenFail)
                    {
                        case WHEN_FAIL.INSTANT:
                            {
                                s.setFail(simconnect);
                                break;
                            }
                        case WHEN_FAIL.TAXI:
                            {
                                if (lSimVars[1].dValue >= 50)
                                    s.setFail(simconnect);
                                break;
                            }
                        case WHEN_FAIL.ALT:
                            {
                                if (lSimVars[0].dValue >= s.failureHeight)
                                    s.setFail(simconnect);
                                break;
                            }
                        case WHEN_FAIL.TIME:
                            {
                                if (flyTime >= s.failureTime)
                                    s.setFail(simconnect);
                                break;
                            }
                    }
                }
            }
        }

        public void stopTimers()
        {
            GroundTimer.Stop();
            ContinTimer.Stop();
            lWillFailSV.Clear();
            flyTime = 0;
        }

        public string Disconnect()
        {
            Console.WriteLine("Disconnect");

            GroundTimer.Stop();
            ContinTimer.Stop();

            if (simconnect != null)
            {
                /// Dispose serves the same purpose as SimConnect_Close()
                simconnect.Dispose();
                simconnect = null;
            }

            return "SimConnect disconnected";
        }

        #endregion

        #region simconnect
        private void SimConnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            uint iRequest = data.dwRequestID;
            //uint iObject = data.dwObjectID;

            foreach (SimVar oSimvarRequest in lSimVars)
            {
                if (iRequest == (uint)oSimvarRequest.eRequest)
                {
                    double dValue = (double)data.dwData[0];
                    oSimvarRequest.dValue = dValue;
                }
            }
        }

        public int GetUserSimConnectWinEvent()
        {
            return WM_USER_SIMCONNECT;
        }

        public void ReceiveSimConnectMessage()
        {
            simconnect?.ReceiveMessage();
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("SimConnect_OnRecvOpen");
            Console.WriteLine("Connected to KH");

            //m_oTimer.Start();
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("SimConnect_OnRecvQuit");
            Console.WriteLine("KH has exited");

            Disconnect();
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
            Console.WriteLine("SimConnect_OnRecvException: " + eException.ToString());

            //lErrorMessages.Add("SimConnect : " + eException.ToString());
        }

        public SimConnect GetSimConnect()
        {
            return simconnect;
        }
        #endregion

        #region interact with form
        public List<SimVar> getFailList()
        {
            return lWillFailSV;
        }

        public IEnumerable<Control> GetAll(Control control, Type type)
        {
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetAll(ctrl, type))
                                      .Concat(controls)
                                      .Where(c => c.GetType() == type);
        }

        public void setMaxTime(int t)
        {
            maxTime = t;
        }

        public void setMaxAlt(int a)
        {
            maxAlt = a;
        }

        public void setMaxNoFails(int f)
        {
            maxNoFails = f;
        }
        #endregion
    }
}
