# EasyWebCam

## Introduction

* EasyWebCam is a Unity `WebCamTexture` wrapper written 100% in C#.

## Getting started

Install via UPM package using the git URL below.
```
https://github.com/lklab/EasyWebCam.git?path=EasyWebCam/Assets/EasyWebCam#1.0.0
```

Drag the `EasyWebCam` prefab from the installed package onto your `Canvas`. And then just run it and the WebCam preview will be displayed on the screen.

To use EasyWebCam in your script, use the `EasyWebCam` namespace.
```
using EasyWebCam;
```

You can capture a photo by calling the function below.
```
WebCam.Capture();
```

## Features

* Permission request
* Display the webcam on the UGUI
* Capture

After cloning this repository, you can see an example in the `SampleScene` scene.
