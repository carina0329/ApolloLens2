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
TODO: add a GUI for easy setup
"""

import websockets
import asyncio
import socket
import random
import time
import json
import sys

class ApolloLensVitalsSource(object):
    """Encapsulates ApolloLens Vitals Server."""

    def __init__(self, port):
        """Creates connection parameters."""
        self.ip = "0.0.0.0"
        self.port = port

    def run(self):
        """Runs server."""
        # try:
        # self.server = websockets.serve(handler, self.ip, self.port)
        asyncio.get_event_loop().run_until_complete(
            websockets.serve(self.handler, self.ip, self.port)
        )
        asyncio.get_event_loop().run_forever()
        # except 


        # try:
        #     self.listen()
        # except BrokenPipeError:
        #     self.listen()
        # except Exception as e:
        #     print(e)

    async def handler(self, path, what):
        print('heloo')
        pass


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
    source = ApolloLensVitalsSource(port=10000)
    source.run()
