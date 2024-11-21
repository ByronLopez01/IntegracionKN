from flask import Flask, request, jsonify

app = Flask(__name__)

@app.route('/api', methods=['POST'])
def receive_json():
    # Imprimir la solicitud recibida para depuración
    print("Received request:", request.get_data(as_text=True))
    
    data = request.get_json()

    # Verificar si se ha recibido JSON
    if not data:
        print("No JSON data received")
        return jsonify({'status': 1, 'message': 'No data received'}), 400

    # Imprimir los datos JSON para depuración
    print("Received JSON data:", data)

    # En este caso, vamos a simplemente responder con éxito
    return jsonify({'status': 0, 'message': 'Success'}), 200

if __name__ == '__main__':
    app.run(host="0.0.0.0", port=4000, debug=True)
