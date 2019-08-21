# PrimeDNS: An Automated DNS Resiliency Provider
PrimeDNS is a DNS caching service that helps the user to continually maintain last known good DNS resolutions in the system's hostfile. It has been implemented in C# using dotnet core framework. 

# Getting Started
1. For running PrimeDNS locally:
	- Clone this repository to your local drive
	- If you don’t have dotnet installed,  you can install it here - https://www.microsoft.com/net/download/thank-you/dotnet-runtime-2.1.0-windows-hosting-bundle-installer
	- Once installed, run the following commands
		```
		cd PrimeDNS\PrimeDNS
		dotnet publish --runtime win10-x86
		```
	- This will create a folder \PrimeDNS\PrimeDNS\bin\Debug\netcoreapp2.1\win10-x86 containing the executables needed to run PrimeDNS.

# Configure PrimeDNS to suit your purpose
1. AppSettings.json
	- The folder 'win10-x86' you created contains template AppSettings.json that you should edit to suit your needs.
	- Telemetry options are by default set to false
	- Currently, TTL Updater isn't fully functional, so keep that option as false.

2. Domains.json
	- The folder 'win10-x86' you created contains template Domains.json that you should use to add the domains of interest to you.

3. Data Folder Location
	- The default data (config files) folder is the folder containing PrimeDNS executables. 
	- But PrimeDNS provides you the facility to separate your data folder and we **highly recommend** that you create a separate data folder.
	- To do this copy the **Files**  folder from PrimeDNS\PrimeDNS and paste it into any location that suits you, let's call this location - <primednsdata>
	- Now, edit the AppSettings.json and Domains.json file in this newly created Files folder as suggested above.
	- Once this is done, you can run PrimeDNS by using the following command
		```
		primedns.exe <primednsdata>
		```
	- This will help your team to maintain your own personalized config files that won't get overwritten with every update to PrimeDNS.


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
