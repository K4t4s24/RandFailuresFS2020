﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RandFailuresFS2020
{
    public partial class Form1 : Form
    {
        ISimCon oSimCon = null;

        public Form1()
        {
            InitializeComponent();
            tcFailures.TabPages.Remove(tpEvents);
            oSimCon = new Simcon(this);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Connect")
            {
                statusLabel.Text = oSimCon.Connect();

                if (statusLabel.Text == "SimConnect connected")
                {
                    btnConnect.Text = "Disconnect";
                    btnStart.Enabled = true;
                }
            }
            else
            {
                statusLabel.Text = oSimCon.Disconnect();
                btnConnect.Text = "Connect";
                btnStart.Enabled = false;
                btnStop.Enabled = false;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            oSimCon.setMaxAlt((int)nruMaxAlt.Value);
            oSimCon.setMaxTime((int)nruMaxTime.Value);
            oSimCon.setMaxNoFails((int)nruNoFails.Value);
            oSimCon.prepareFailures();

            btnStop.Enabled = true;
            btnStart.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            oSimCon.stopTimers();
            btnStop.Enabled = false;
            btnStart.Enabled = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            oSimCon.Disconnect();
            Application.Exit();
        }

        private void btnFailList_Click(object sender, EventArgs e)
        {
            List<SimVar> failList = oSimCon.getFailList();

            richTextBox1.Clear();

            string altTime = "";

            foreach (SimVar s in failList)
            {
                if (s.whenFail == WHEN_FAIL.ALT)
                {
                    altTime = "at " + s.failureHeight.ToString() + " ft";
                }
                else if (s.whenFail == WHEN_FAIL.TIME)
                {
                    altTime = "after " + s.failureTime.ToString() + " seconds";
                }

                richTextBox1.Text += "Name: " + s.sName + " when will fail: " + s.whenFail + " " + altTime + "\n";
            }
        }

        public IEnumerable<Control> GetAll(Control control, Type type)
        {
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetAll(ctrl, type))
                                      .Concat(controls)
                                      .Where(c => c.GetType() == type);
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == 0x402)
            {
                if (oSimCon.GetSimConnect() != null)
                {
                    oSimCon.GetSimConnect().ReceiveMessage();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        #region setOptions
        private void nruMaxAlt_ValueChanged(object sender, EventArgs e)
        {
            oSimCon.setMaxAlt((int)nruMaxAlt.Value);
        }

        private void nruMaxTime_ValueChanged(object sender, EventArgs e)
        {
            oSimCon.setMaxTime((int)nruMaxTime.Value);
        }

        private void nruNoFails_ValueChanged(object sender, EventArgs e)
        {
            oSimCon.setMaxNoFails((int)nruNoFails.Value);
        }

        private void GitLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GitLink.LinkVisited = true;

            System.Diagnostics.Process.Start("https://github.com/kanaron/RandFailuresFS2020");
        }
        #endregion

        #region setAllBox
        private void nruAllPanel_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in tpPanel.Controls)
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruAllPanel.Value;
            }
        }

        private void nruE1All_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbEngine1, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruE1All.Value;
            }
        }

        private void nruE2All_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbEngine2, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruE2All.Value;
            }
        }

        private void nruE3All_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbEngine3, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruE3All.Value;
            }
        }

        private void nruE4All_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbEngine4, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruE4All.Value;
            }
        }

        private void nruEnginesAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in tpEngine1.Controls)
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruEnginesAll.Value;
            }
        }

        private void nruAvionicsAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbAvionics, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruAvionicsAll.Value;
            }
        }

        private void nruGearAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbGear, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruGearAll.Value;
            }
        }

        private void nruFuelAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbFuel, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruFuelAll.Value;
            }
        }

        private void nruBrakesAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbBrakes, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruBrakesAll.Value;
            }
        }

        private void nruOtherAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in GetAll(gbOther, typeof(NumericUpDown)))
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruOtherAll.Value;
            }
        }

        private void nruSystemAll_ValueChanged(object sender, EventArgs e)
        {
            foreach (Control c in tpAvionics.Controls)
            {
                if (c is NumericUpDown)
                    ((NumericUpDown)c).Value = nruSystemAll.Value;
            }
        }
        #endregion

    }
}
