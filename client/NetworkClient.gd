extends Node

const HOST = "127.0.0.1"
const PORT = 8080

var socket: StreamPeerTCP = StreamPeerTCP.new()
var is_conected_to_server: bool = false

func _ready() -> void:
	print("[CLIENTE] Tentando conexão com o servidor")
	var error = socket.connect_to_host(HOST,PORT)
	
	if error != OK:
		print("[CLIENTE] Erro ao conectar", error)
	
func _process(delta: float) -> void:
	socket.poll()
	
	var status = socket.get_status()
	
	match status:
		StreamPeerTCP.STATUS_CONNECTING:
			pass
			
		StreamPeerTCP.STATUS_CONNECTED:
			if !is_conected_to_server:
				is_conected_to_server = true
				send_menssage("Olá mundo")
			check_server_response()
			
		StreamPeerTCP.STATUS_ERROR:
			print("[CLIENTE] Erro ao conectar")
			
func send_menssage(msg: String):
	var payload = {
		"type": "mensagem",
		"msg": msg
	}
	
	var json_string = JSON.stringify(payload) + "\n"
	var bytes = json_string.to_utf8_buffer()
	
	socket.put_data(bytes)
	print("[CLIENTE] Envio uma mensagem")
	
func check_server_response():
	var available_bytes = socket.get_available_bytes()
	
	if available_bytes > 0:
		var data = socket.get_data(available_bytes)
		if data[0] == OK:
			var response = data[1].get_string_from_utf8()
			print("[CLIENTE] Resposta do Servidor: ", response.strip_edges())
