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

## Image Gallery
### HoloLens application to upload and view images. The user will first be directed to a screen to choose an image file, either from the local system or from the server.  Once an appropriate image has been chosen (PNG, JPG, JPEG, BMP, TIFF, GIF), it will be displayed.  Image Gallery is only for use with the previously mentioned image file types.  Medical images in the DICOM format should utilize the Scan Gallery application instead.  Currently, Image Gallery can pull images from the local system and display them for view.  This application will be one of two parts in the future to enable a remote user to send notes and marked-up screenshots to the HoloLens user.  In Progress: replacing the displayed image with a new image that the user selects and allowing the user to zoom in on the image.
=======
## For Sending Files Between Users:
  ### On Remote User: 
    Open a terminal and run the bash script host with the file path of the file you want to transfer (ie: ./host [filepath])
  ### On HoloLens Computer:
    Open a terminal and run the bash script remote with the public ip address of the user running host (ie: ./remote [public ip address]).
    This has to be done while host is being run.
