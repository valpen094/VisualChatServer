from faster_whisper import WhisperModel
from flask import Flask, jsonify, request

server = Flask(__name__)

@server.route('/api/faster-whisper/transcribe', methods=['POST'])
def transcribe():
    data = request.json
    file_path = data.get("filePath", "")
    return jsonify({"message": f"{file_path}"})

if __name__ == '__main__':
    server.run(debug=True, host="localhost", port=5000)