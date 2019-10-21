# Dr. HoloLens - How to Deploy on Laptop and HoloLens
### Requirements:
1. The solutions must build to this version: version 1809 / build number 17763
(Target Version 10.0.17763.0)
2. Must build using Visual Studio 2019

## Deploying an App
1.	Select ‘Clean Solution’ under ‘Build’ menu after every update in code.
2.	Debug using x86 and the desired build target (remote machine, connected device, local machine).
a.	Remote Machine
i.	HoloLens must be on the same network as computer running Visual Studio 2019
ii.	Input the IP address of HoloLens into solution properties
iii.	Right click on desired application (ApolloLensClient for example)
iv.	Select ‘Deploy’
v.	Once the bottom bar says ‘Deploy Complete,’ use the Debug bar at the top to select the green play button to the left of the desired build target
vi.	The app will deploy on the HoloLens
b.	Connected Device
i.	Connect the HoloLens to the computer using the provided power cord
ii.	Right click on desired application (ApolloLensClient for example)
iii.	Select ‘Deploy’
iv.	Once the bottom bar says ‘Deploy Complete,’ use the Debug bar at the top to select the green play button to the left of the desired build target
v.	The app will deploy on the HoloLens
c.	Local Machine
i.	Use the Debug bar at the top to select the green play button to the left of the desired build target
ii.	The app will deploy on the computer in use

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
5. Audio will be streamed between both devic
