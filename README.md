# Dr. HoloLens - How to Deploy on Laptop and HoloLens
### Requirements:
1. The solutions must build to this version: version 1809 / build number 17763
(Target Version 10.0.17763.0)
2. Must build using Visual Studio 2019

## There is also a HTML version of this tutorial: open tutorial.html on a web browser

## Deploying an App
1. Select ‘Clean Solution’ under ‘Build’ menu after every update in code.
2. Debug using x86 and the desired build target (remote machine, connected device, local machine).
- Remote Machine
  - HoloLens must be on the same network as computer running Visual Studio 2019
  - Input the IP address of HoloLens into solution properties
  - Right click on desired application (ApolloLensClient for example)
  - Select ‘Deploy’
  - Once the bottom bar says ‘Deploy Complete,’ use the Debug bar at the top to select the green play button to the left of the desired build target
  - The app will deploy on the HoloLens
- Connected Device
  - Connect the HoloLens to the computer using the provided power cord
  - Right click on desired application (ApolloLensClient for example)
  - Select ‘Deploy’
  - Once the bottom bar says ‘Deploy Complete,’ use the Debug bar at the top to select the green play button to the left of the desired build target
  - The app will deploy on the HoloLens
- Local Machine
  - Use the Debug bar at the top to select the green play button to the left of the desired build target
  - The app will deploy on the computer in use

### Connect Laparoscopic Video to HoloLens
1. Deploy the ApolloLensSource on a computer hooked up to laparoscope
2. Deploy the ApolloLensClient to the HoloLens
3. Select desired camera to send to HoloLens
4. Select the 'connect to source' button on ApolloLensClient to view the video feed

### Connect Vitals to HoloLens
1. Deploy the ApolloLensVitals to the HoloLens
2. Sample data will be displayed on the app (more to come in Beta)

### Connect Audio Between Remote Viewer and HoloLens
1. Deploy the ApolloLensAudio on a computer
2. Deploy the ApolloLensAudio to the HoloLens
3. Select 'Set Up Remote Call' button on both devices
4. Select the 'Start Call' on ONE device (it does not matter which one)
5. Audio will be streamed between both devices
loLensAudio on a computer
2. Deploy the ApolloLensAudio to the HoloLens
3. Select 'Set Up Remote Call' button on both devices
4. Select the 'Start Call' on ONE device (it does not matter which one)
5. Audio will be streamed between both devic
# THEIALENS INSTRUCTIONS

## For Running Video Calling:
  ### On HoloLens: 
    Turn on HoloLens and connect it to the network.
    Obtain IP Address From Settings > Network & Internet > Wi-Fi > Advanced Options. 
  ### On Remote Computer: 
    Connect to the Network. Download and Open the Microsoft HoloLens Companion App from the Microsoft Windows App Store. 
    Click the "Add" button to add the HoloLens and enter the IP Address obtained from the HoloLens.  
    Add any other credentials needed (username and password), which are created by the user. 
    Click the new connection made with the HoloLens.
    Click video stream to watch the video stream.
    Click the camera button to take a screenshot of what is being seen.
                      
## For Running Audio Calling:
  ### On HoloLens:
    Turn on HoloLens and connect it to the network.
    Open the Dynamic 365 Application and choose a user to call.
  ### On Computer: 
    Open up Microsoft Teams.
    Accept a call when the HoloLens User calls you.
               
## For Sending Files Between Users:
  ### On Remote User:
    Open a terminal and run the bash script host with the file path of the file you want to transfer (ie: ./host [filepath])
  ### On HoloLens Computer:
    Open a terminal and run the bash script remote with the public ip address of the user running host (ie: ./remote [public ip address]). This has to be done while host is being run.

## Image Gallery:
  HoloLens application to upload and view images. The user will first be directed to a screen to choose an image file, either from the local system or from the server.  Once an appropriate image has been chosen (PNG, JPG, JPEG, BMP, TIFF, GIF), it will be displayed.  Image Gallery is only for use with the previously mentioned image file types.  Medical images in the DICOM format should utilize the Scan Gallery application instead.  Currently, Image Gallery can pull images from the local system and display them for view.  This application will be one of two parts in the future to enable a remote user to send notes and marked-up screenshots to the HoloLens user.  In Progress: replacing the displayed image with a new image that the user selects and allowing the user to zoom in on the image.
