

use std::{ffi::c_uchar, slice};

use wl_clipboard_rs::copy::{MimeType, Options};



#[unsafe(no_mangle)]
pub unsafe extern fn copy(data: *const c_uchar, data_length: u32) {
    let a = unsafe { slice::from_raw_parts(data,data_length.try_into().unwrap()) };
    let opts = Options::new();
    let _ = opts.copy(wl_clipboard_rs::copy::Source::Bytes(a.into()), MimeType::Autodetect);
}