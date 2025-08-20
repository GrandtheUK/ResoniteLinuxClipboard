using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using FrooxEngine;
using ResoniteModLoader;
using Elements.Assets;
using System.Threading.Tasks;
using Elements.Core;
using System.Reflection;

namespace ResoniteLinuxClipboard;

public class ResoniteLinuxClipboard : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; 
	public override string Name => "ResoniteLinuxClipboard";
	public override string Author => "Grand";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/resonite-modding-group/ExampleMod/";

	public override void OnEngineInit() {
		Engine.Current.RunPostInit( () => {
			Engine.Current.InputInterface.RegisterClipboardInterface(new ResoniteLinuxClipboardInterface());
			Msg("clipboard registered");
		});
		// Console.WriteLine("mod loaded but not active");

	}
	
}
public partial class ResoniteLinuxClipboardInterface : IClipboardInterface {
	public bool ContainsText => false;

	public bool ContainsFiles => false;

	public bool ContainsImage => false;

	public void Dispose() {}

	public async Task<string> GetText() {
		await Task.Run(() => {});
		return "";
	}

	public Task<List<string>> GetFiles() {
		UniLog.Log("attempting get files");
		UniLog.Flush();
		return Task.FromResult<List<string>>([]);
	}

	public Task<Bitmap2D> GetImage() {
		UniLog.Log("attempting get image");
		UniLog.Flush();
		return Task.FromResult<Bitmap2D>(null);
	}

	public Task<bool> SetText(string text) {
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		Copy(bytes, (uint)bytes.Length);
		UniLog.Log("Attempted copy text");
		UniLog.Flush();
		return Task.FromResult(true);
	}

	public Task<bool> SetBitmap(Bitmap2D bitmap) {
		int count = bitmap.ElementTotalBytes;
		byte[] bytes = bitmap.RawData.ToArray();
		Copy(bytes,(uint)count);
		UniLog.Log("Attempted copy image");
		UniLog.Flush();
		return Task.FromResult(true);
	}


	[LibraryImport("../rml_libs/resoniteclipboard_rs", EntryPoint = "copy")]
	private static partial void Copy(byte[] data, uint data_length);
}