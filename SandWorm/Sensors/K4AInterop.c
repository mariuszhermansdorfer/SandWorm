#include <stdio.h>
#include <stdlib.h>
#include <k4a/k4a.h>


uint32_t device_count = k4a_device_get_installed_count();
printf("Found %d connected devices:\n", device_count);

// Open first Kinect for Azure device
if (device_count != 1)
{
    printf("Unexpected number of devices found (%d)\n", device_count);
    goto Exit;
}

if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &device))
{
    printf("Failed to open device\n");
    goto Exit;
}

// Configure the image sensor; see enums at:
// https://microsoft.github.io/Azure-Kinect-Sensor-SDK/master/group___enumerations.html
k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
config.camera_fps = K4A_FRAMES_PER_SECOND_15;
config.color_format = K4A_IMAGE_FORMAT_COLOR_MJPG;
config.color_resolution = K4A_COLOR_RESOLUTION_2160P;
config.depth_mode = K4A_DEPTH_MODE_WFOV_UNBINNED;

if (K4A_RESULT_SUCCEEDED != k4a_device_start_cameras(device, &config))
{
    printf("Failed to start device\n");
    goto Exit;
}

// Capture a depth frame
switch (k4a_device_get_capture(device, &capture, TIMEOUT_IN_MS))
{
	case K4A_WAIT_RESULT_SUCCEEDED:
		k4a_image_t image = k4a_capture_get_depth_image(capture);
		if (image != NULL)
		{
			printf(" | Depth16 res:%4dx%4d stride:%5d\n", k4a_image_get_height_pixels(image),
														  k4a_image_get_width_pixels(image),
														  k4a_image_get_stride_bytes(image));

			// TODO: transform to point cloud data structure; see
			// https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/examples/fastpointcloud/main.cpp#L11

			// Release the image
			k4a_image_release(image);
		}
		// Release the capture
		k4a_capture_release(capture);
	case K4A_WAIT_RESULT_TIMEOUT:
		printf("Timed out waiting for a capture\n");
		continue;
		break;
	case K4A_WAIT_RESULT_FAILED:
		printf("Failed to read a capture\n");
		goto Exit;
}