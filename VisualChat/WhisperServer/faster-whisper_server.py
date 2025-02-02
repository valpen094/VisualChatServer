from faster_whisper import WhisperModel
import socket
import datetime
import sounddevice as sd
import numpy as np
import scipy.io.wavfile as wav
import time

# Recording
def record():
    SAMPLE_RATE = 44000
    THRESHOLD = 500
    SILENCE_DURATION = 1
    OUTPUT_FILE = "audio.wav"

    audio_data = []
    start_time = time.time()
    recoginazed_voice = False

    print(f"{datetime.datetime.now()} Start recording.")

    while True:
        # Get audio every 100ms
        chunk = sd.rec(int(SAMPLE_RATE * 0.1), samplerate = SAMPLE_RATE, channels = 1, dtype = np.int16)
        sd.wait()
    
        # Calculating volume
        volume = np.abs(chunk).mean()

        # Save a data (After initial recognition)
        if recoginazed_voice:
            audio_data.append(chunk)

        # Silence detection
        if volume < THRESHOLD:
            if time.time() - start_time > SILENCE_DURATION:
                if recoginazed_voice: # If there is no sound after recording starts, the condition will not be met.
                    print(f"{datetime.datetime.now()} Stop recording.")
                    break

        else:
            # Save a data (Initial recognition)
            if recoginazed_voice is False:
                audio_data.append(chunk)

            start_time = time.time()
            recoginazed_voice = True

    audio_data = np.concatenate(audio_data, axis = 0)

    # Save the file
    wav.write(OUTPUT_FILE, SAMPLE_RATE, audio_data)
    return OUTPUT_FILE

# Transcribe an audio file
def transcribe(file_path):
    MODEL_SIZE = "small"

    # os.environ["XDG_CACHE_HOME"] = "."

    # model = WhisperModel(MODEL_SIZE, device="cuda", compute_type="float16")
    model = WhisperModel(MODEL_SIZE, device="cpu", compute_type="float32")

    # Transcription of audio files
    segments, info = model.transcribe(file_path, beam_size=5)

    # Transcription results
    message = ""
    for segment in segments:
        print(f"{datetime.datetime.now()} [{segment.start:.2f}s - {segment.end:.2f}s] {segment.text}")
        message += segment.text
        
    return message

# Start the server
def serve():
    HOST = "localhost"
    PORT = 5023

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((HOST, PORT))
    server_socket.listen()

    print(f"{datetime.datetime.now()} Server listening on {HOST}:{PORT}")

    client_socket, addr = server_socket.accept()
    print(f"{datetime.datetime.now()} Connected by {addr}")

    while True:
        try:
            print(f"{datetime.datetime.now()} Waiting to receive...")

            # Receive from client
            data = client_socket.recv(1024).decode("utf-8")
            if not data:
                continue
        
            print(f"{datetime.datetime.now()} Received from client: {data}")

            if data.strip().lower() == "transcribe":
                # Recording
                audio_file = record()

                # Convert voice data to text  
                response = transcribe(audio_file)

                # Send a response data
                client_socket.sendall(response.encode("utf-8"))

            # Terminate connection
            elif data.strip().lower() == "close":
                print(f"{datetime.datetime.now()} Received close command. Shutting down server.")
                break

        except Exception as e:
            print(f"{datetime.datetime.now()} Error: {e}")

    client_socket.close()
    server_socket.close()
    print(f"{datetime.datetime.now()} Server closed.")

if __name__ == "__main__":
    while True:
        serve()