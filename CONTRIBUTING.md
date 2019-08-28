# Contributing

Hi there :blush: 

We're thrilled that you'd like to contribute to this project. Your help is essential for taking PrimeDNS forward!

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


# How can you help?

Currently, we have identified 3 main bugs/enhancements where we are looking for help from awesome people like you  -

1. [TTL Updater is not functional](https://github.com/microsoft/PrimeDNS/issues/1)
	We would ideally like PrimeDNS to query the authoritative nameservers and update the domain's TTL once in say few hours. Currently, this part of the code is disfunctional and so a default TTL of 300 is assumed for all domains. 
2. [HostfileUpdater: Should Update only when there is a change that needs updating.](https://github.com/microsoft/PrimeDNS/issues/2)
	Only recently did we realize that whenever hostfile is edited, local DNS cache gets flushed :worried: This means, 
	1. We should minimalize the number of edits to hostfile to the best we can. 
	2. We should dynamically detect the critical domains list (instead of a static input list) so that PrimeDNS can take care of the top few hundred domains without affecting the cache or latency of any. 
3. [Dynamic Detection of Input Domains](https://github.com/microsoft/PrimeDNS/issues/3)
	To those looking to solve something new and interesting, this might be a great place to start! 
	We want to figure out how to dynamically identify the critical domains list for any PC at any given time and then be able to make that a part of PrimeDNS by making the code dotnet core compatible. Isn't this exciting? 

# Contact

As we can see, PrimeDNS needs a lot more work to become really useful and so thank you for coming forward to help :relaxed:

PrimeDNS is maintained with :heart: by @arunothia
