"""
server.py
============
Dr. HoloLens
============
Jimil Patel <pateljim@umich.edu>
David Nie <dnie@umich.edu>
Laura McCallum <laumccal@umich.edu>
Dylan McCallister <djmcalli@umich.edu>

Websocket based server.
Captures vitals from the machines and transmits to HoloLens.

TODO: use real vitals values
TODO: encryption process (implement verify_client, symmetric encryption?)
TODO: determine appropriate buffer size and authentication process
"""

import socket
import random
import time
import json
import sys

class ApolloLensVitalsSource(object):
    """Represents Vitals Source Server."""

    def __init__(self, port):
        """Initializes socket and binds to appropriate IP/port."""
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.port = port
        self.addr = ("0.0.0.0", port)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.sock.bind(self.addr)

    def run(self):
        """Run server."""
        try:
            self.listen()
        except BrokenPipeError:
            self.listen()
        except Exception as e:
            print(e)


    def listen(self):
        """Waits for client connection and accepts only if client verified."""
        self.sock.listen(1)
        print("listening")
        # loop in place in case connection is lost
        while True:
            self.connection, self.client_addr = self.sock.accept()
            print("connected.")
            print(self.client_addr)
            try:
                chunks = self.connection.recv(4096)
                if not self.verify_client(chunks):
                    self.connection.close()
                self.stream()
            except:
                self.connection.close()

    def verify_client(self, chunks):
        """Verifies the client for security purposes."""
        print(chunks)
        return True
                
    def stream(self):
        """Continuously streams vitals to the verified client."""
        while True:
            time.sleep(0.5)
            message = {
                "heart_rate": random.randint(60, 101),
                "blood_pressure_systolic": random.randint(118, 121),
                "blood_pressure_diastolic": random.randint(79, 81),
                "respiration_rate": random.randint(12, 21)
            }
            enc_msg = json.dumps(message).encode('utf-8')
            print(sys.getsizeof(enc_msg))
            self.connection.sendall(enc_msg)


if __name__ == "__main__":
    source = ApolloLensVitalsSource(10000)
    source.run()
