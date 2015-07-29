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
	public class RawAsyncSerialNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Input")]
		public IDiffSpread<Stream> FStreamIn;

		[Input("Enable")]
		public IDiffSpread<bool> Enable;

		[Output("Output")]
		public ISpread<Stream> FStreamOut;

		
		[Import()]
		ILogger Logger;
		//when dealing with byte streams (what we call Raw in the GUI) it's always
		//good to have a byte buffer around. we'll use it when copying the data.
		readonly byte[] FBuffer = new byte[1024];
		#endregion fields & pins

		ASerialPort Port;

		Stream sBuffer = new MemoryStream();
		
		void SetupDelegates() {
			Port.DataReceived += delegate(byte[] data) {
				// handle data
				//Logger.Log(LogType.Debug, "Received: "+data.Length.ToString());
				sBuffer.Write(data, 0, data.Length);
			};
			
			FStreamIn.Changed += delegate(IDiffSpread<Stream> input) {
				if (input.SliceCount == 0) return;
				Stream s = input[0];
				int count = (int)s.Length;
				s.Read(FBuffer, 0, count);
				Port.Write(FBuffer, 0, count);
			};
			
			Enable.Changed += delegate(IDiffSpread<bool> enabled) {
				if (enabled.SliceCount == 0) return;
				
				Port.Enable = enabled[0];
			};
		}
		
		//called when all inputs and outputs defined above are assigned from the host
		public void OnImportsSatisfied()
		{
			//start with an empty stream output
			FStreamOut.SliceCount = 0;
			
			Port = new ASerialPort();
			Port.BaudRate = 9600;
			
			Port.PortName = "COM4";
			
			FStreamOut.SliceCount = 1;
			
			SetupDelegates();
		}

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			FStreamOut.ResizeAndDispose(1, () => new MemoryStream());
			Stream s = new MemoryStream();
			sBuffer.CopyTo(s);
			FStreamOut[0] = s;
			sBuffer.SetLength(0);
		}
	}
}
