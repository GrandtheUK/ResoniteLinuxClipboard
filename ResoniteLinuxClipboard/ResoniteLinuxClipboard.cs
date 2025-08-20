using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Elements.Assets;
using System.Threading.Tasks;
using Elements.Core;
using System.IO;

namespace ResoniteLinuxClipboard;

public class ResoniteLinuxClipboard : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0";
	public override string Name => "ResoniteLinuxClipboard";
	public override string Author => "Grand";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/GrandtheUK/ResoniteLinuxClipboard";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.GrandtheUK.ResoniteLinuxClipboard");
		harmony.PatchAll();
	}
}

[HarmonyPatch]
public static class RenderSystemPatches {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(RenderSystem), "RegisterBootstrapperClipboardInterface")]
	public static void RegisterClipboard_Postfix() {
		IClipboardInterface? OriginalClipboardInterface = Engine.Current.InputInterface.Clipboard;
		if (OriginalClipboardInterface != null) {
		}
		ResoniteLinuxClipboard.Msg("Registering clipboard");
		AccessTools.PropertySetter(typeof(InputInterface), "Clipboard").Invoke(
			Engine.Current.InputInterface, [new ResoniteLinuxClipboardInterface(OriginalClipboardInterface)]
		);
		ResoniteLinuxClipboard.Msg("Clipboard registered");
	}
}

public class ResoniteLinuxClipboardInterface : IClipboardInterface {
	private IClipboardInterface? OriginalClipboardInterface;

	public bool ContainsText => true;

	public bool ContainsFiles => false;

	public bool ContainsImage => false;

	public ResoniteLinuxClipboardInterface(IClipboardInterface? OriginalClipboardInterface = null) {
		this.OriginalClipboardInterface = OriginalClipboardInterface;
	}

	public void Dispose() {
		ResoniteLinuxClipboard.Msg("disposing");
	}

	public Task<string> GetText() {
		ResoniteLinuxClipboard.Msg("Attempting get text");
		return OriginalClipboardInterface?.GetText() ?? Task.FromResult<string>(null);
	}

	public Task<List<string>> GetFiles() {
		ResoniteLinuxClipboard.Msg("Attempting get files");
		return OriginalClipboardInterface?.GetFiles() ?? Task.FromResult<List<string>>([]);
	}

	public Task<Bitmap2D> GetImage() {
		ResoniteLinuxClipboard.Msg("Attempting get image");
		return OriginalClipboardInterface?.GetImage() ?? Task.FromResult<Bitmap2D>(null);
	}

	public Task<bool> SetText(string text) {
		ResoniteLinuxClipboard.Msg("Attempting copy text");
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		ExternalFunctions.Copy(bytes, (uint)bytes.Length, 1);
		return Task.FromResult(true);
	}

	public Task<bool> SetBitmap(Bitmap2D bitmap) {
		ResoniteLinuxClipboard.Msg("Attempting copy image");
		int count = bitmap.ElementTotalBytes;
		byte[] bytes = bitmap.RawData.ToArray();
		ExternalFunctions.Copy(bytes, (uint)count,4);
		return Task.FromResult(true);
	}
}

internal partial class ExternalFunctions {
	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy")]
	public static partial void Copy(byte[] data, uint data_length, uint mimetype);
}
