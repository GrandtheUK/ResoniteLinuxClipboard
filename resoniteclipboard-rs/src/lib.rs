

use std::slice;

use wl_clipboard_rs::copy::{MimeType, Options};



#[unsafe(no_mangle)]
pub extern "C" fn copy(data: *const u8, data_length: u32, mimetype: u32) {
    let a = unsafe { slice::from_raw_parts(data,data_length.try_into().unwrap()) };
    let opts = Options::new();
    let mime = match mimetype {
        1 => MimeType::Text,
        2 => MimeType::Specific("image/png".to_string()),
        3 => MimeType::Specific("image/jpeg".to_string()),
        4 => MimeType::Specific("image/webp".to_string()),
        _ => MimeType::Autodetect
    };
    match opts.copy(wl_clipboard_rs::copy::Source::Bytes(a.into()), mime) {
        Ok(_) => println!("copy success"),
        Err(_) => todo!("copy failure"),
    }
}