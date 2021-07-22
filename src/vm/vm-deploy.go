package main

import (
	"fmt"
	"io"
	"net/http"
	"os"
	"os/exec"
)

func main() {
	if _, err := os.Stat("C:\\Program Files\\TightVNC\\tvnserver.exe"); err == nil {
		fmt.Println("TightVNC is already installed!")
		// checks if TightVNC server executable exists, if not it installs it
	} else if os.IsNotExist(err) {
		vncUrl := "https://www.tightvnc.com/download/2.8.59/tightvnc-2.8.59-gpl-setup-64bit.msi"
		DownloadFile("vnc.msi", vncUrl)
		fmt.Println("Downloaded: " + vncUrl)
		// downloads tightvnc setup
		cmd := exec.Command("msiexec", "/i", "vnc.msi")
		cmd.Run()
		cmd.Start()
		// executes it
		if _, err := os.Stat("C:\\Program Files\\TightVNC\\tvnserver.exe"); err == nil {
			fmt.Println("TightVNC Executable has been found.")
		} else if os.IsNotExist(err) {
			fmt.Println("Something went wrong and the TightVNC Executable could not be found!")
			fmt.Scanln()
			os.Exit(1)
		}
		// checks if it was correctly installed
		os.Remove("vnc.msi")
		// removes vnc.msi since its not needed anymore
	}
	if _, err := os.Stat("C:\\Program Files\\VM Battle Royale\\ngrok.exe"); err == nil {
		fmt.Println("ngrok Executable already present!")
		// checks if vmbr ngrok executable exists, if not it downloads it
	} else if os.IsNotExist(err) {
		os.Mkdir("C:\\Program Files\\VM Battle Royale", os.ModeDir)
		ngrUrl := "https://github.com/guest1352/stuff/releases/download/waas/ngrok.exe"
		DownloadFile("C:\\Program Files\\VM Battle Royale\\ngrok.exe", ngrUrl)
		fmt.Println("Downloaded: " + ngrUrl)
		// create vmbr directory and download ngrok executable to it
	}
	fmt.Println("Enter ngrok AuthToken")
	var ngrauthtoken string
	fmt.Scanln(&ngrauthtoken)
	cmdd := exec.Command("C:\\Program Files\\VM Battle Royale\\ngrok.exe", "authtoken", ngrauthtoken)
	cmdd.Run()
	cmdd.Start()
	// sets the ngrok authtoken

	// now download the c# vm program thingy thatll launch vnc and ngrok on boot and all that other stuff it needs to do
	// then execute it obviously
}

// DownloadFile will download a url to a local file. It's efficient because it will
// write as it downloads and not load the whole file into memory.
func DownloadFile(filepath string, url string) error {

	// Get the data
	resp, err := http.Get(url)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	// Create the file
	out, err := os.Create(filepath)
	if err != nil {
		return err
	}
	defer out.Close()

	// Write the body to file
	_, err = io.Copy(out, resp.Body)
	return err
}
