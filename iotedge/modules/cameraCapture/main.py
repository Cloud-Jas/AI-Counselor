import json
from azure.iot.device import IoTHubModuleClient, Message
import time
import sys
import os
import requests

SENT_IMAGES = 0
AGGREGATION_INTERVAL = 60
aggregated_data = {"total_images": 0, "emotions": []}


CLIENT = None

FACE_EMOTION_RECOGNIZER_URL = "http://faceemotionrecognizer/image"  # Replace with the actual URL of your FaceEmotionRecognizer module

def send_to_hub(str_message):
    message = Message(bytearray(str_message, 'utf8'))
    CLIENT.send_message_to_output(message, "output1")
    global SENT_IMAGES
    SENT_IMAGES += 1
    print("Total images sent: {}".format(SENT_IMAGES))

def capture_image(image_path):    
    os.system("fswebcam -r 1280x720 --no-banner {}".format(image_path))
    print("Captured image successfully")

def send_image_for_processing(image_path):
    headers = {'Content-Type': 'application/octet-stream'}

    with open(image_path, mode="rb") as image_file:
        try:
            response = requests.post(FACE_EMOTION_RECOGNIZER_URL, headers=headers, data=image_file.read())
            print("Response from classification service: ({}) {}".format(response.status_code, response.text))
            return response.json()
        except Exception as e:
            print(e)
            print("No response from classification service")
            return None    
        
def find_last_emotion_entry():
    if aggregated_data["emotions"]:
        return aggregated_data["emotions"][-1]
    return None


def main():
    try:
        print("Simulated camera module for Azure IoT Edge. Press Ctrl-C to exit.")

        global CLIENT
        CLIENT = IoTHubModuleClient.create_from_edge_environment()

        print("The sample is now sending images for processing and will indefinitely.")

        start_time = time.time()
        prev_emotion = None
        timestamp_range_start = None

        while True:            
            image_path = "captured_image.jpg"
            capture_image(image_path)                
            classification = send_image_for_processing(image_path)
            if classification:
                predictions = classification.get('predictions', [])
                max_prediction = max(predictions, key=lambda x: x.get('probability', 0.0), default=None)
                if max_prediction:
                    tag_name = max_prediction.get('tagName', 'unknown')
                    probability = max_prediction.get('probability', 0.0)
                    emotion_entry = find_last_emotion_entry()
                    if emotion_entry and tag_name == prev_emotion:                        
                        emotion_entry["timestamp_range"]["end"] = time.time()                        
                    else:                                             
                        timestamp_range_start = time.time()
                        aggregated_data["emotions"].append({
                            "emotion": tag_name,                            
                            "timestamp_range": {"start": timestamp_range_start, "end": time.time()}
                        })
                        prev_emotion = tag_name                    
                    aggregated_data["total_images"] += 1

            elapsed_time = time.time() - start_time
            if elapsed_time >= AGGREGATION_INTERVAL:             
                send_aggregated_data()
                start_time = time.time()
                aggregated_data["total_images"] = 0
                aggregated_data["emotions"] = []

            time.sleep(1)

    except KeyboardInterrupt:
        print("IoT Edge module sample stopped")

    finally:
        if CLIENT:
            CLIENT.disconnect()

def send_aggregated_data():
    if aggregated_data["emotions"]:
        aggregated_data_json = json.dumps({
            "total_images": aggregated_data["total_images"],
            "emotions": aggregated_data["emotions"]
        })
        print(aggregated_data_json)
        send_to_hub(aggregated_data_json)
        print("Sent to IoT Hub")

if __name__ == '__main__':
    try:
        main()
    except Exception as e:
        print("Unexpected error:", str(e))
    finally:
        if CLIENT:
            CLIENT.disconnect()
