﻿using SimpleOsciloscope.UI.HardwareInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace SimpleOsciloscope.UI
{
    /// <summary>
    /// Interaction logic for Calibration.xaml
    /// </summary>
    public partial class Calibration : Window
    {
        public Calibration()
        {
            InitializeComponent();

			this.DataContext = this.Context = new CalibrationContext();
        }


		public static void Calibrate(int channel, string portname)
		{
			var wnd = new Calibration();

			wnd.Context.Init(portname,false);

			wnd.ShowDialog();


			//wnd.Context._10xLwGnd = 10;
			//wnd.Context._10xLwVcc = 4080;
			//wnd.Context._10xHiGnd = 10;
			//wnd.Context._10xHiVcc = 4080;

			{//check for AC/DC switch
				var err1 = wnd.Context._10xLwGnd - wnd.Context._10xLwVcc;

				if (Math.Abs(err1) < 50)
				{
					//ac/dc switch is active
					MessageBox.Show("Seems the AC/DC switch is in ON mode.\r\nToggle the AC/DC switch and try again...");
					return;
				}
            }

            double a1, a2, b1, b2;

			RpiPicoDaqInterface.GetCalibrationParameters(0, wnd.Context._10xLwGnd, 3.3, wnd.Context._10xLwVcc,
				out a1, out b1);

			RpiPicoDaqInterface.GetCalibrationParameters(0, wnd.Context._10xHiGnd, 3.3, wnd.Context._10xHiVcc,
				out a2, out b2);

			var fl = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

			var xml = new XmlDocument();
			xml.Load(fl);



			{
                //Can ConfigurationManager retain XML comments on Save()?
				//short answer is no!
                //https://stackoverflow.com/questions/1954358/can-configurationmanager-retain-xml-comments-on-save
				//have to write it ourself


                var na1 = xml.SelectSingleNode("configuration/appSettings/add[@key='ch1_alpha_off']");
                na1.Attributes["value"].Value = a1.ToString();

                var nb1 = xml.SelectSingleNode("configuration/appSettings/add[@key='ch1_beta_off']");
                nb1.Attributes["value"].Value = b1.ToString();

                var na2 = xml.SelectSingleNode("configuration/appSettings/add[@key='ch1_alpha_on']");
                na2.Attributes["value"].Value = a2.ToString();

                var nb2 = xml.SelectSingleNode("configuration/appSettings/add[@key='ch1_beta_on']");
                nb2.Attributes["value"].Value = b2.ToString();

				xml.Save(fl);
            }
           

        }


        CalibrationContext Context;


        public class CalibrationContext : INotifyPropertyChanged
		{
			#region INotifyPropertyChanged members and helpers

			public event PropertyChangedEventHandler PropertyChanged;

			protected static bool AreEqualObjects(object obj1, object obj2)
			{
				var obj1Null = ReferenceEquals(obj1, null);
				var obj2Null = ReferenceEquals(obj2, null);

				if (obj1Null && obj2Null)
					return true;

				if (obj1Null || obj2Null)
					return false;

				if (obj1.GetType() != obj2.GetType())
					return false;

				if (ReferenceEquals(obj1, obj2))
					return true;

				return obj1.Equals(obj2);
			}

			protected void OnPropertyChanged(params string[] propertyNames)
			{
				if (propertyNames == null)
					return;

				if (this.PropertyChanged != null)
					foreach (var propertyName in propertyNames)
						this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}

			#endregion


			public string PortName;

			public void Init(string portName,bool gnd)
            {
				PortName = portName;
				Intfs = new HardwareInterface.RpiPicoDaqInterface(portName, 0);

                Intfs.Connect();
				Intfs.StopAdc();

				RefreshGpioStatus();

				ConnectProbeVcc = !(ConnectProbeGnd = gnd);


				Target10xBtn = false;
                ConnectProbeGnd = true;
                ConnectProbeVcc = false;
            }


			public void RefreshGpioStatus()
			{
				var pins = new byte[] { 19, 20 };

				if (!Intfs.IsConnected)
				{
					Intfs.Connect();
					Intfs.StopAdc();
				}

				Thread.Sleep(100);

				Intfs.StopAdc();

				var vals = Intfs.GetGpioValues(pins);

				Intfs.DisConnect();


                this.Status10xBtn = vals[0];
                this.StatusAcdcBtn = vals[1];

				this.Toggle10xBtn = Status10xBtn != Target10xBtn;
				this.ToggleAcdcBtn = StatusAcdcBtn;

				this.CanContinue = !this.Toggle10xBtn;
            }

			public HardwareInterface.RpiPicoDaqInterface Intfs;

            #region TargetChannel Property and field

            [Obfuscation(Exclude = true, ApplyToMembers = false)]
			public int TargetChannel
			{
				get { return _TargetChannel; }
				set
				{
					if (AreEqualObjects(_TargetChannel, value))
						return;

					var _fieldOldValue = _TargetChannel;

					_TargetChannel = value;

					CalibrationContext.OnTargetChannelChanged(this, new PropertyValueChangedEventArgs<int>(_fieldOldValue, value));

					this.OnPropertyChanged("TargetChannel");
				}
			}

			private int _TargetChannel;

			public EventHandler<PropertyValueChangedEventArgs<int>> TargetChannelChanged;

			public static void OnTargetChannelChanged(object sender, PropertyValueChangedEventArgs<int> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.TargetChannelChanged != null)
					obj.TargetChannelChanged(obj, e);
			}

			#endregion

			#region Status10xBtn Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool Status10xBtn
			{
				get { return _Status10xBtn; }
				set
				{
					if (AreEqualObjects(_Status10xBtn, value))
						return;

					var _fieldOldValue = _Status10xBtn;

					_Status10xBtn = value;

					CalibrationContext.OnStatus10xBtnChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("Status10xBtn");
				}
			}

			private bool _Status10xBtn;

			public EventHandler<PropertyValueChangedEventArgs<bool>> Status10xBtnChanged;

			public static void OnStatus10xBtnChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.Status10xBtnChanged != null)
					obj.Status10xBtnChanged(obj, e);
			}

			#endregion

			#region Target10xBtn Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool Target10xBtn
			{
				get { return _Target10xBtn; }
				set
				{
					if (AreEqualObjects(_Target10xBtn, value))
						return;

					var _fieldOldValue = _Target10xBtn;

					_Target10xBtn = value;

					CalibrationContext.OnTarget10xBtnChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("Target10xBtn");
				}
			}

			private bool _Target10xBtn;

			public EventHandler<PropertyValueChangedEventArgs<bool>> Target10xBtnChanged;

			public static void OnTarget10xBtnChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.Target10xBtnChanged != null)
					obj.Target10xBtnChanged(obj, e);
			}

			#endregion

			#region Toggle10xBtn Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool Toggle10xBtn
			{
				get { return _Toggle10xBtn; }
				set
				{
					if (AreEqualObjects(_Toggle10xBtn, value))
						return;

					var _fieldOldValue = _Toggle10xBtn;

					_Toggle10xBtn = value;

					CalibrationContext.OnToggle10xBtnChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("Toggle10xBtn");
				}
			}

			private bool _Toggle10xBtn;

			public EventHandler<PropertyValueChangedEventArgs<bool>> Toggle10xBtnChanged;

			public static void OnToggle10xBtnChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.Toggle10xBtnChanged != null)
					obj.Toggle10xBtnChanged(obj, e);
			}

			#endregion

			#region StatusAcdcBtn Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool StatusAcdcBtn
			{
				get { return _StatusAcdcBtn; }
				set
				{
					if (AreEqualObjects(_StatusAcdcBtn, value))
						return;

					var _fieldOldValue = _StatusAcdcBtn;

					_StatusAcdcBtn = value;

					CalibrationContext.OnStatusAcdcBtnChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("StatusAcdcBtn");
				}
			}

			private bool _StatusAcdcBtn;

			public EventHandler<PropertyValueChangedEventArgs<bool>> StatusAcdcBtnChanged;

			public static void OnStatusAcdcBtnChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.StatusAcdcBtnChanged != null)
					obj.StatusAcdcBtnChanged(obj, e);
			}

			#endregion

			#region ToggleAcdcBtn Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool ToggleAcdcBtn
			{
				get { return _ToggleAcdcBtn; }
				set
				{
					if (AreEqualObjects(_ToggleAcdcBtn, value))
						return;

					var _fieldOldValue = _ToggleAcdcBtn;

					_ToggleAcdcBtn = value;

					CalibrationContext.OnToggleAcdcBtnChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("ToggleAcdcBtn");
				}
			}

			private bool _ToggleAcdcBtn;

			public EventHandler<PropertyValueChangedEventArgs<bool>> ToggleAcdcBtnChanged;

			public static void OnToggleAcdcBtnChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.ToggleAcdcBtnChanged != null)
					obj.ToggleAcdcBtnChanged(obj, e);
			}

			#endregion

			#region ConnectProbeGnd Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool ConnectProbeGnd
			{
				get { return _ConnectProbeGnd; }
				set
				{
					if (AreEqualObjects(_ConnectProbeGnd, value))
						return;

					var _fieldOldValue = _ConnectProbeGnd;

					_ConnectProbeGnd = value;

					CalibrationContext.OnConnectProbeGndChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("ConnectProbeGnd");
				}
			}

			private bool _ConnectProbeGnd;

			public EventHandler<PropertyValueChangedEventArgs<bool>> ConnectProbeGndChanged;

			public static void OnConnectProbeGndChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.ConnectProbeGndChanged != null)
					obj.ConnectProbeGndChanged(obj, e);
			}

			#endregion

			#region ConnectProbeVcc Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool ConnectProbeVcc
			{
				get { return _ConnectProbeVcc; }
				set
				{
					if (AreEqualObjects(_ConnectProbeVcc, value))
						return;

					var _fieldOldValue = _ConnectProbeVcc;

					_ConnectProbeVcc = value;

					CalibrationContext.OnConnectProbeVccChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));
						

					this.OnPropertyChanged("ConnectProbeVcc");
				}
			}

			private bool _ConnectProbeVcc;

			public EventHandler<PropertyValueChangedEventArgs<bool>> ConnectProbeVccChanged;

			public static void OnConnectProbeVccChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.ConnectProbeVccChanged != null)
					obj.ConnectProbeVccChanged(obj, e);
			}

			#endregion

			#region ProbeTarget Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public string ProbeTarget
			{
				get { return _ProbeTarget; }
				set
				{
					if (AreEqualObjects(_ProbeTarget, value))
						return;

					var _fieldOldValue = _ProbeTarget;

					_ProbeTarget = value;

					CalibrationContext.OnProbeTargetChanged(this, new PropertyValueChangedEventArgs<string>(_fieldOldValue, value));

					this.OnPropertyChanged("ProbeTarget");
				}
			}

			private string _ProbeTarget;

			public EventHandler<PropertyValueChangedEventArgs<string>> ProbeTargetChanged;

			public static void OnProbeTargetChanged(object sender, PropertyValueChangedEventArgs<string> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.ProbeTargetChanged != null)
					obj.ProbeTargetChanged(obj, e);
			}

			#endregion

			#region CanContinue Property and field

			[Obfuscation(Exclude = true, ApplyToMembers = false)]
			public bool CanContinue
			{
				get { return _CanContinue; }
				set
				{
					if (AreEqualObjects(_CanContinue, value))
						return;

					var _fieldOldValue = _CanContinue;

					_CanContinue = value;

					CalibrationContext.OnCanContinueChanged(this, new PropertyValueChangedEventArgs<bool>(_fieldOldValue, value));

					this.OnPropertyChanged("CanContinue");
				}
			}

			private bool _CanContinue;

			public EventHandler<PropertyValueChangedEventArgs<bool>> CanContinueChanged;

			public static void OnCanContinueChanged(object sender, PropertyValueChangedEventArgs<bool> e)
			{
				var obj = sender as CalibrationContext;

				if (obj.CanContinueChanged != null)
					obj.CanContinueChanged(obj, e);
			}

            #endregion


            public double _10xLwVcc;//adc value when 10x btn is low, and probe connected to vcc
            public double _10xLwGnd;

            public double _10xHiVcc;
            public double _10xHiGnd;

        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
			this.Context.Intfs.DisConnect();

			var center = AdcSampler.GetAdcMedian(Context.Intfs.PortName);

            if (Context.Status10xBtn == true)//high
            {
                if (this.Context.ConnectProbeGnd)
                    Context._10xHiGnd = center;

                if (this.Context.ConnectProbeVcc)
                    Context._10xHiVcc = center;
            }
			else
			{
                if (this.Context.ConnectProbeGnd)
                    Context._10xLwGnd = center;

                if (this.Context.ConnectProbeVcc)
                    Context._10xLwVcc = center;
            }

			var lvl = (Context.Target10xBtn ? 2 : 0) + (this.Context.ConnectProbeVcc ? 1 : 0);

			if (lvl == 3)
			{
                this.Close();
				return;
            }
				

			var nxt = lvl + 1;

            Context.Target10xBtn = nxt >= 2;
            Context.ConnectProbeGnd = nxt%2 == 0;
			Context.ConnectProbeVcc = !Context.ConnectProbeGnd;

			Context.RefreshGpioStatus();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
			Context.RefreshGpioStatus();
        }
    }
}
