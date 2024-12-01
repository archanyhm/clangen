use std::{env, fs, io, thread, time};
use std::fs::{File, OpenOptions};
use std::io::Write;
use std::os::windows::process::CommandExt;
use std::path::Path;
use std::process::Command;

fn recursive_copy(source: impl AsRef<Path>, destination: impl AsRef<Path>) -> io::Result<()> {
    fs::create_dir_all(&destination)?;

    for entry in fs::read_dir(source)? {
        let entry = entry?;
        let file_type = entry.file_type()?;
        let file_name = entry.file_name();

        if file_type.is_dir() {
            recursive_copy(entry.path(), destination.as_ref().join(file_name))?;
        } else {
            fs::copy(entry.path(), destination.as_ref().join(file_name))?;
        }
    }

    Ok(())
}

fn move_update_files_win() -> io::Result<()> {
    println!("Starting update process");
    thread::sleep(time::Duration::from_secs(2));

    let args: Vec<String> = env::args().collect();
    let game_path = Path::new(&args[1]);

    let entries = fs::read_dir(game_path)?;

    println!("Deleting old game files");
    for entry in entries {
        let entry = entry?;
        let file_name = entry.file_name();
        let file_name_str = file_name.to_str().unwrap();

        if file_name_str == "Downloads" { continue; }

        let file_type = entry.file_type()?;
        if file_type.is_dir() {
            fs::remove_dir_all(entry.path())?
        } else {
            if let Err(_) = fs::remove_file(entry.path()) {
                println!("Can't delete file {}", file_name_str);
            }
        }
    }

    let download_path = game_path.join("Downloads").join("Clangen");
    println!("Applying updated files");
    
    let entries = fs::read_dir(&download_path)?;
    for entry in entries {
        let entry = entry?;
        let file_name = entry.file_name();
        let file_name_str = file_name.to_str().unwrap();

        if file_name_str == "Downloads" { continue; }

        let file_type = entry.file_type()?;
        let target_path = if file_name_str == "_internal" {
            game_path.join("_internal")
        } else {
            game_path.join(file_name_str)
        };

        if file_type.is_dir() {
            recursive_copy(entry.path(), target_path)?;
        } else {
            fs::copy(entry.path(), target_path)?;
        }
    }

    OpenOptions::new().create(true).write(true).open(game_path.join("auto-updated"))?;

    println!("Starting game");
    Command::new("cmd.exe")
        .args(["/C", "start", "Clangen.exe"])
        .current_dir(game_path)
        .creation_flags(0x00000008 | 0x01000000)
        .spawn()?;

    Ok(())
}

fn main() {
    if let Err(x) = move_update_files_win() {
        let mut file = File::create("self_updater.log").unwrap();
        file.write_all(x.to_string().as_ref()).unwrap();
    }
}