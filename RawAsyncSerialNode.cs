#region usings
using System;
using System.IO;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using Muthesius.SickLRF;

#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "AsyncSerial", Category = "Raw", Help = "Basic raw template which copies up to count bytes from the input to the output", Tags = "")]
	#endregion PluginInfo
	public class RawAsyncSerialNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Input")]
		public IDiffSpread<Stream> FStreamIn;
		
		[Input("Port Name", DefaultString = "")]
		public IDiffSpread<string> Portname;
		
		[Input("Baudrate", DefaultValue = 9600)]
		public IDiffSpread<int> Baudrates;
		
		[Input("Enable")]
		public IDiffSpread<bool> Enable;
		
		[Output("Output")]
		public ISpread<Stream> FStreamOut;
		
		[Output("Enabled")]
		public ISpread<bool> IsEnabled;
		
		
		[Import()]
		ILogger Logger;
		
		
		readonly byte[] FBuffer = new byte[1024];
		#endregion fields & pins
		
		ASerialPort Port;
		
		Stream sBuffer = new MemoryStream();
		
		public void Dispose() {
			try {
				Port.Enable = false;
			} finally {
				Port.Close();
				Port.Dispose();
			}
		}
		
		void SetupDelegates() {
			
			Port.DidError += delegate(Exception e) {
				Logger.Log(LogType.Debug, "---------");
				Logger.Log(e);
				Logger.Log(LogType.Debug, "---------");
			};
			
			Baudrates.Changed += delegate(IDiffSpread<int> rates) {
				if (rates.SliceCount == 0) return;
				bool state = Port.IsOpen;
				if (state) Port.Enable = false;
				Port.BaudRate = rates[0];
				if(state) Port.Enable = true;
			};
			
			Portname.Changed += delegate(IDiffSpread<string> names) {
				if (names.SliceCount == 0) return;
				if (names[0] == "") return;
				bool state = Port.IsOpen;
				if (state) Port.Enable = false;
				Port.PortName = names[0];
				if(state) Port.Enable = true;
			};
			
			Enable.Changed += delegate(IDiffSpread<bool> enabled) {
				if (enabled.SliceCount == 0) return;
				Port.Enable = enabled[0];
			};
			
			Port.DataReceived += delegate(byte[] data) {
				// handle data
				try {
					sBuffer.Write(data, 0, data.Length);
				} catch (Exception e) {
					Logger.Log(e);
				}
			};
			
			FStreamIn.Changed += delegate(IDiffSpread<Stream> input) {
				if ( !Port.IsOpen || input.SliceCount == 0) return;
				Stream s = input[0];
				int count = (int)s.Length;
				s.Read(FBuffer, 0, count);
				Port.Write(FBuffer, 0, count);
			};
		}
		
		//called when all inputs and outputs defined above are assigned from the host
		public void OnImportsSatisfied()
		{
			Port = new ASerialPort();
			FStreamOut.SliceCount = 1;
			
			SetupDelegates();
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			IsEnabled.SliceCount = 1;
			IsEnabled[0] = Port.IsOpen;
			
			FStreamOut[0] = new MemoryStream();
			try {
				byte[] buffer = new  byte[sBuffer.Length];
				int count = (int) sBuffer.Length;
				sBuffer.Read(buffer,0,count);
				FStreamOut[0].Write(buffer,0,count);
				FStreamOut[0].Flush();
				sBuffer.SetLength(0);
			} catch (Exception e) {
				Logger.Log(e);
			}
			//
		}
	}
}
