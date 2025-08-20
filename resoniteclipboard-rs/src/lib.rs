use std::ffi::{CStr, c_char, c_uchar};
use std::slice;

use wl_clipboard_rs::copy::{MimeType, Options};

#[unsafe(no_mangle)]
pub extern "C" fn copy(data: *const c_uchar, data_length: u32) {
    let data_array = unsafe { slice::from_raw_parts(data, data_length.try_into().unwrap()) };
    match Options::new().copy(
        wl_clipboard_rs::copy::Source::Bytes(data_array.into()),
        MimeType::Autodetect,
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
        MimeType::Text,
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
        Ok(s) => MimeType::Specific(s.to_string()),
        Err(_) => MimeType::Autodetect,
    };
    match Options::new().copy(wl_clipboard_rs::copy::Source::Bytes(a.into()), mime_type) {
        Ok(_) => println!("copy success"),
        Err(_) => todo!("copy failure"),
    }
}
