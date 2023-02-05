# Info:
- Student : Darren Siriram (0999506)
- Student : Ertugrul Karaduman (0997475)



## Software requirement:
- .NET >6 

## How to run using IDEA
- Clone the repo to your machine.
- Open the repo with Rider or visual studio code.
- open the solution
- Run Server side first
- Run Client side after.

## How to run using Terminal
- open a terminal and navigate to the repo.
- navigate to the file_download_server folder and type: dotnet run
- navigate to the client folder and type dotnet run

## Notice!
```
when u occur the error that the file is not found. make sure that you have set your working directory
to the the folder of file_download_server in your IDEA.
```


To run the test-server to test your client implementation.
# Setup and run (running docker environment)
## Install
If not already installed go to the website of docker and install it on your machine.
Once docker is installed and started follow the steps bellow to connect your client implementation to the server.
### Notice! 
Make sure to ```enable hyper V``` in your windows settings if you are using windows and install with```hyper-v``` instead of ```WSL2```

## Step 1: Load
<!-- navigate in your command line to directory where the udp_server-image.tar image is stored -->
<!-- type the following command in your command line to load the image-->
<!-- load and run image from URL as .tar -->
### Macos Command: 
docker load < udp_server-image.tar
### Windows Command:
docker load --input udp_server-image.tar
## Step 2: Check
<!-- type the following command in your command line to see if the image is now enlisted. Your docker image name must match with the image name of the command in step 3. If you are using docker desktop you should be able to see it in the images section  -->
docker image ls
## Step 2: Run
<!-- run the image as a container that listens to UPD protocol on port 5004. Be aware the UDP must be included in the command otherwise it will listen to TCP by default and the server will not communicate with your client -->
<!-- server -->
docker run -i -t -p 127.0.0.1:32000:32000/udp udp_server-image:latest
<!-- Once a container is created you can use docker desktop to rerun the container and test your implementation. There is no need to execute the command above again as it will create a new container with a different name every time -->