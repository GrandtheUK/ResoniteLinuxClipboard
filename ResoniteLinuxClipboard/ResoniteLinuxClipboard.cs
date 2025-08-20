using System.Collections.Generic;
using System.Runtime.InteropServices;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Elements.Assets;
using System.Threading.Tasks;
using System.IO;

namespace ResoniteLinuxClipboard;

public class ResoniteLinuxClipboard : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0";
	public override string Name => "ResoniteLinuxClipboard";
	public override string Author => "Grand";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/GrandtheUK/ResoniteLinuxClipboard";

	public static ModConfiguration Config;

	public override void OnEngineInit() {
		Config = GetConfiguration();
		Config.Save(true);

		Harmony harmony = new Harmony("com.GrandtheUK.ResoniteLinuxClipboard");
		harmony.PatchAll();
	}

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<CopyImageFormatEnum> CopyImageFormatKey = new("copy_image_format", "Format in which to export images", () => CopyImageFormatEnum.WEBP);

	public static CopyImageFormatEnum CopyImageFormat => Config.GetValue(CopyImageFormatKey);
}

public enum CopyImageFormatEnum {
	WEBP,
	PNG,
	JPG,
}

[HarmonyPatch]
public static class RenderSystemPatches {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(RenderSystem), "RegisterBootstrapperClipboardInterface")]
	public static void RegisterClipboard_Postfix() {
		IClipboardInterface? OriginalClipboardInterface = Engine.Current.InputInterface.Clipboard;
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
		ResoniteLinuxClipboard.Msg("Attempting get text (fallback on original clipboard interface)");
		return OriginalClipboardInterface?.GetText() ?? Task.FromResult<string>(null);
	}

	public Task<List<string>> GetFiles() {
		ResoniteLinuxClipboard.Msg("Attempting get files (not implemented yet)");
		return OriginalClipboardInterface?.GetFiles() ?? Task.FromResult<List<string>>([]);
	}

	public Task<Bitmap2D> GetImage() {
		ResoniteLinuxClipboard.Msg("Attempting get image (not implemented yet)");
		return OriginalClipboardInterface?.GetImage() ?? Task.FromResult<Bitmap2D>(null);
	}

	public Task<bool> SetText(string text) {
		ResoniteLinuxClipboard.Msg("Attempting copy text");
		ExternalFunctions.CopyText(text);
		return Task.FromResult(true);
	}

	public Task<bool> SetBitmap(Bitmap2D bitmap) {
		ResoniteLinuxClipboard.Msg("Attempting copy image");
		string mimeType = MimeTypeFromEnum(ResoniteLinuxClipboard.CopyImageFormat);
		string extension = ExtensionFromEnum(ResoniteLinuxClipboard.CopyImageFormat);
		int quality = QualityFromEnum(ResoniteLinuxClipboard.CopyImageFormat);
		return Task.Run(() => {
			using (MemoryStream stream = new()) {
				bitmap.Save(stream, extension, quality);
				ExternalFunctions.CopyWithType(stream.ToArray(), (uint)stream.Length, mimeType);
			}
			return true;
		});
	}

	private static int QualityFromEnum(CopyImageFormatEnum mimeTypeEnum) {
		return mimeTypeEnum switch {
			CopyImageFormatEnum.JPG => 85,
			_ => 101, // Lossless
		};
	}

	private static string MimeTypeFromEnum(CopyImageFormatEnum mimeTypeEnum) {
		return mimeTypeEnum switch {
			CopyImageFormatEnum.WEBP => "image/webp",
			CopyImageFormatEnum.JPG => "image/jpeg",
			_ => "image/png",
		};

	}

	private static string ExtensionFromEnum(CopyImageFormatEnum mimeTypeEnum) {
		return mimeTypeEnum switch {
			CopyImageFormatEnum.WEBP => "webp",
			CopyImageFormatEnum.JPG => "jpeg",
			_ => "png",
		};

	}
}

internal partial class ExternalFunctions {
	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy_text", StringMarshalling = StringMarshalling.Utf8)]
	public static partial void CopyText(string data);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy_auto")]
	public static partial void CopyAuto(byte[] data, uint data_length);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy_with_type", StringMarshalling = StringMarshalling.Utf8)]
	public static partial void CopyWithType(byte[] data, uint data_length, string mime_type);
}
