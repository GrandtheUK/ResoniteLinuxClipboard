using System.Collections.Generic;
using System.Runtime.InteropServices;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Elements.Assets;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;
using Elements.Core;
using System.Text;
using System.Runtime.CompilerServices;

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
		Engine.Current.InputInterface.Clipboard?.Dispose();
		ResoniteLinuxClipboard.Msg("Registering clipboard");
		AccessTools.PropertySetter(typeof(InputInterface), "Clipboard").Invoke(
			Engine.Current.InputInterface, [new ResoniteLinuxClipboardInterface()]
		);
		ResoniteLinuxClipboard.Msg("Clipboard registered");
	}
}

public class ResoniteLinuxClipboardInterface : IClipboardInterface {
	private static readonly string[] TEXT_TYPES = {
		"UTF8_STRING",
		"TEXT",
		"STRING"
	};

	private static readonly string[] IMAGE_PRIORITY_TYPES = {
		"image/png",
		"image/webp",
		"image/jpeg",
		"image/bmp"
	};

	private static bool IsText(string mimeType) {
		return mimeType.StartsWith("text/") || TEXT_TYPES.Contains(mimeType);
	}

	public bool ContainsText => AvailableMimeTypes.Any(IsText);

	public bool ContainsFiles => AvailableMimeTypes.Contains("text/uri-list");

	public bool ContainsImage => AvailableMimeTypes.Any(t => t.StartsWith("image/"));

	public void Dispose() {
		ResoniteLinuxClipboard.Msg("disposing");
	}

	public Task<string> GetText() {
		ResoniteLinuxClipboard.Msg("Attempting get text");
		return Task.Run(() => {
			IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(int));
			IntPtr content = ExternalFunctions.PasteText(sizePtr);
			return ConsumeStringPtr(content, sizePtr);
		});
	}

	public Task<List<string>> GetFiles() {
		ResoniteLinuxClipboard.Msg("Attempting get files");
		return Task.Run(() => {
			IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(uint));
			IntPtr content = ExternalFunctions.PasteWithType("text/uri-list", sizePtr);
			return ConsumeStringPtr(content, sizePtr)
				.Trim().Split("\n").Where(x => x.Length > 0)
				.Select(x => x.Replace("file://", "")).ToList();
		});
	}

	public Task<Bitmap2D> GetImage() {
		ResoniteLinuxClipboard.Msg("Attempting get image (DOES NOT WORK YET)");
		return Task.Run<Bitmap2D>(() => {
			string preferredMimeType = ImageMimeTypePriority(AvailableMimeTypes);
			ResoniteLinuxClipboard.Msg($"Preferred MIME type: {preferredMimeType}");

			IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(uint));
			IntPtr content = ExternalFunctions.PasteWithType(preferredMimeType, sizePtr);
			int size = ConsumeSizePtr(sizePtr);

			Bitmap2D? result = null;
			if (content == IntPtr.Zero) {
				return result;
			}

			if (size <= 0) {
				unsafe { NativeMemory.Free(content.ToPointer()); }
				return result;
			}

			Bitmap2D bitmap;
			unsafe {
				using (UnmanagedMemoryStream data = new((byte*)content.ToPointer(), size)) {
					bitmap = Bitmap2D.Load(data, preferredMimeType.Replace("image/", ""), true);
				}
				NativeMemory.Free(content.ToPointer());
			}
			return bitmap;
		});
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

	private static string ImageMimeTypePriority(List<string> types) {
		return IMAGE_PRIORITY_TYPES.FirstOrDefault(types.Contains, types.First());
	}

	private static string ConsumeStringPtr(IntPtr ptr, IntPtr sizePtr) {
		int size = ConsumeSizePtr(sizePtr);

		if (ptr == IntPtr.Zero) {
			return "";
		}

		string? result = null;
		if (size > 0) {
			result = Marshal.PtrToStringAuto(ptr, size);
		}
		unsafe { NativeMemory.Free(ptr.ToPointer()); }
		return result ?? "";
	}

	private static int ConsumeSizePtr(IntPtr sizePtr) {
		int size = Marshal.ReadInt32(sizePtr);
		Marshal.FreeHGlobal(sizePtr);
		return size;
	}
	
	private static List<string> AvailableMimeTypes {
		get {
			IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(uint));
			IntPtr content = ExternalFunctions.AvailableMimeTypes(sizePtr);
			return ConsumeStringPtr(content, sizePtr).Trim().Split("\n").Where(x => x.Length > 0).ToList();
		}
	}
}

internal partial class ExternalFunctions {
	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy_text", StringMarshalling = StringMarshalling.Utf8)]
	public static partial void CopyText(string data);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy_auto")]
	public static partial void CopyAuto(byte[] data, uint data_length);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "copy_with_type", StringMarshalling = StringMarshalling.Utf8)]
	public static partial void CopyWithType(byte[] data, uint data_length, string mime_type);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "available_mime_types")]
	public static unsafe partial IntPtr AvailableMimeTypes(IntPtr sizePtr);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "paste_text")]
	public static unsafe partial IntPtr PasteText(IntPtr sizePtr);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "paste_auto")]
	public static unsafe partial IntPtr PasteAuto(IntPtr sizePtr);

	[LibraryImport("resoniteclipboard_rs", EntryPoint = "paste_with_type", StringMarshalling = StringMarshalling.Utf8)]
	public static unsafe partial IntPtr PasteWithType(string mime_type, IntPtr sizePtr);
}
