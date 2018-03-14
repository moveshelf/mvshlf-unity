# mvshlf-unity

Moveshelf is a cloud-based 3D digital motion visualization service and collaboration tool, created to make human motion data more accessible and easier to use. Find out more about [Moveshelf](https://moveshelf.com)

## Moveshelf API integration for Unity

### Goal
The Moveshelf-Unity integration is open source (MIT license) and its purpose is primarily to show how easy it is to integrate directly with Moveshelf's API. It is not meant as a fully featured produciton ready tool. If you can contribute to this project and make it better, please do! We will be there to help. Then again, if you just want to grab this and build your own propietary tool, that is fine too.

### Getting started:
* Clone the repository inside the *Assets/* folder of a Unity project;
* Request an invitation on https://moveshelf.com, get started and upload your motion data files;
* Create your first Moveshelf API key and configure the Unity plugin to immediately use it;   
*NOTE: after login, API keys can be revoked on moveshelf.com/profile/[your-username]*
* Search and access motion clips, notes, comments and previews from Moveshelf, all without leaving Unity!

If all the steps are completed, your Moveshelf-Unity integration should be all set. 

If you feel like, go ahead and deep dive our source code, fork this repository and show us how to get the best out of your Motion Data using Moveshelf API.


### About Moveshelf API:
Moveshelf API is designed with [GraphQL](http://graphql.org/) to offer greatest flexibility to each API request, ensure faster development cycles and provide developers a more consistent interface with higher maintainability.   
GraphQL lets you replace multiple REST requests with a single call to fetch the data you specify and only that data. This is a very powerful advantage over the REST API endpoints.   
   
We encourage you to look into the source code of this plugin to see how motion clips information is requested using our API. For more advanced API usage, please refer for example to the interactive comments feed handling. [Learn more about GraphQL](http://graphql.org/learn/)


### FAQ:
* Moveshelf supports both FBX and BVH motion data formats, however, since Unity only [supports FBX](https://docs.unity3d.com/Manual/3D-formats.html), search results are hereby filtered to only find animations supported from the engine.
* For security reasons, Movehself API key’s can’t be downloaded after creation. Make sure you store them immediately at  time of creation in a secure place.
* As usual, after motion data is imported in Unity, make sure imported animation type is configured as 'Humanoid' and that Unity Mecanim system is nicely mapping skeleton bones in the correct way. [Read more about Unity mechanim](https://unity3d.com/learn/tutorials/topics/animation/animate-anything-mecanim)
