﻿using SimConModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RandFailuresFS2020_WPF.Models
{
    public class OverviewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _stateText;
        private Brush _stateColor;
        private bool _resetEnabled;
        private List<PresetModel> _presetsList;
        private PresetModel _selectedPreset;
        private int _selectedItemPreset;


        public List<PresetModel> PresetsList
        {
            get { return _presetsList; }
            set
            {
                _presetsList = value;
                NotifyPropertyChanged();
            }
        }
        public PresetModel SelectedPreset
        {
            get { return _selectedPreset; }
            set
            {
                _selectedPreset = value;
                NotifyPropertyChanged();
                if (value != null)
                    SQLOptions.UpdateOption("PresetID", value.PresetID.ToString());
            }
        }
        public int SelectedItemPreset
        {
            get { return _selectedItemPreset; }
            set
            {
                _selectedItemPreset = value;
                NotifyPropertyChanged();
            }
        }
        public string StateText
        {
            get { return _stateText; }
            set
            {
                _stateText = value;
                NotifyPropertyChanged();
            }
        }
        public Brush StateColor
        {
            get { return _stateColor; }
            set
            {
                _stateColor = value;
                NotifyPropertyChanged();
            }
        }
        public bool ResetEnabled
        {
            get { return _resetEnabled; }
            set
            {
                _resetEnabled = value;
                NotifyPropertyChanged();
            }
        }


        public OverviewModel()
        {
            SimCon.GetSimCon().StateChanged += OverviewPresenter_StateChanged;
            StateText = "Sim not found";
            StateColor = Brushes.Red;
            ResetEnabled = false;
            Reload();
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Reload()
        {
            PresetsList = SQLPresets.LoadPresets();
            SelectedItemPreset = PresetsList.FindIndex(a => a.PresetID.Equals(SQLOptions.LoadOptionValueInt("PresetID")));
        }

        private void OverviewPresenter_StateChanged(object? sender, string e)
        {
            StateText = e;
            switch (e)
            {
                case "Sim not found":
                    {
                        StateColor = Brushes.Red;
                        break;
                    }
                case "Sim connected":
                    {
                        StateColor = Brushes.Green;
                        break;
                    }
                case "Failures started":
                    {
                        ResetEnabled = true;
                        break;
                    }
            }
        }
    }
}
