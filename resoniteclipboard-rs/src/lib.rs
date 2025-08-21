use std::ffi::{CStr, c_char, c_uchar};
use std::io::Read;
use std::ptr::null;
use std::slice;

use wl_clipboard_rs::copy::{self, Options};
use wl_clipboard_rs::paste::{self, ClipboardType, Seat, get_contents, get_mime_types};

#[unsafe(no_mangle)]
pub extern "C" fn copy_auto(data: *const c_uchar, data_length: u32) {
    let data_array = unsafe { slice::from_raw_parts(data, data_length.try_into().unwrap()) };
    match Options::new().copy(
        wl_clipboard_rs::copy::Source::Bytes(data_array.into()),
        copy::MimeType::Autodetect,
    ) {
        Ok(_) => println!("copy success"),
        Err(_) => todo!("copy failure"),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn copy_text(data: *const c_char) {
    let data_cstr = unsafe { CStr::from_ptr(data) };
    match Options::new().copy(
        wl_clipboard_rs::copy::Source::Bytes(Box::from(data_cstr.to_bytes())),
        copy::MimeType::Text,
    ) {
        Ok(_) => println!("copy success"),
        Err(_) => todo!("copy failure"),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn copy_with_type(
    data: *const c_uchar,
    data_length: u32,
    mime_type_raw: *const c_char,
) {
    let a = unsafe { slice::from_raw_parts(data, data_length.try_into().unwrap()) };
    let mime_type_cstr = unsafe { CStr::from_ptr(mime_type_raw) };
    let mime_type = match mime_type_cstr.to_str() {
        Ok(s) => copy::MimeType::Specific(s.to_string()),
        Err(_) => copy::MimeType::Autodetect,
    };
    match Options::new().copy(wl_clipboard_rs::copy::Source::Bytes(a.into()), mime_type) {
        Ok(_) => println!("copy success"),
        Err(_) => todo!("copy failure"),
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn available_mime_types(size: *mut u32) -> *const c_uchar {
    let mime_types = match get_mime_types(ClipboardType::Regular, Seat::Unspecified) {
        Ok(t) => t,
        Err(_) => {
            println!("get mime types failure");
            return null();
        }
    };
    let concatenated = mime_types.into_iter().fold("".to_string(), |mut acc, t| {
        acc.push_str(&t);
        acc.push('\n');
        acc
    });
    unsafe { size.write(concatenated.len().try_into().unwrap()) }
    let allocated = unsafe { libc::malloc(concatenated.len()) };
    let allocated_slice = unsafe {
        std::slice::from_raw_parts_mut::<u8>(allocated.cast(), concatenated.as_bytes().len())
    };
    allocated_slice.copy_from_slice(concatenated.as_bytes());
    allocated.cast()
}

#[unsafe(no_mangle)]
pub extern "C" fn paste_with_type(mime_type_raw: *const c_char, size: *mut u32) -> *const c_uchar {
    let mime_type_cstr = unsafe { CStr::from_ptr(mime_type_raw) };
    let mime_type = match mime_type_cstr.to_str() {
        Ok(s) => paste::MimeType::Specific(s),
        Err(_) => paste::MimeType::Any,
    };
    let (result, length) = paste(mime_type);
    unsafe { size.write(length.try_into().unwrap()) }
    result
}

#[unsafe(no_mangle)]
pub extern "C" fn paste_auto(size: *mut u32) -> *const c_uchar {
    let (result, length) = paste(paste::MimeType::Any);
    unsafe { size.write(length.try_into().unwrap()) }
    result
}

#[unsafe(no_mangle)]
pub extern "C" fn paste_text(size: *mut u32) -> *const c_uchar {
    let (result, length) = paste(paste::MimeType::Text);
    unsafe { size.write(length.try_into().unwrap()) }
    result
}

fn paste(mime_type: paste::MimeType) -> (*const c_uchar, usize) {
    match get_contents(ClipboardType::Regular, Seat::Unspecified, mime_type) {
        Ok((mut p, _)) => {
            let mut contents = vec![];
            match p.read_to_end(&mut contents) {
                Ok(_) => {
                    let allocated = unsafe { libc::malloc(contents.len()) };
                    let allocated_slice = unsafe {
                        std::slice::from_raw_parts_mut::<u8>(allocated.cast(), contents.len())
                    };
                    allocated_slice.copy_from_slice(&contents);
                    (allocated.cast(), contents.len())
                }
                Err(_) => todo!("paste failure"),
            }
        }
        Err(_) => todo!("paste failure"),
    }
}
