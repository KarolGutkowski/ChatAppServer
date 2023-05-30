<h1>ChatAppServer</h1>

<h3>Quick summary</h3>
<ul>
  <li>This server allow users to connect to chatting room.</li>
  <li>Process of connection and disconnection is asynchronous</li>
  <li>In order to join the chat room users have to gain authorization by entering valid user data which is compared to data stored in database</li>
  <li>The server serves as middle man in chat room sending messages from sender to receivers (note that sender doesnt receive his message back)</li>
  <li>For testing this project i used my own client app (repo: https://github.com/KarolGutkowski/ChatServerClient)
</ul>


Here are basic examples of how the app works:

Start of the app and one user enterings his data:
![login](https://user-images.githubusercontent.com/90787864/236683040-d8e4cf1b-1aeb-4f92-b013-581242d21851.png)

User login screen
![2](https://user-images.githubusercontent.com/90787864/236683046-9ee19481-16f4-4d38-8372-3a202259626c.png)

Snapshot of mid converstation. Note that user2 joined to converstation later so his chat log is a bit shorter:
![3](https://user-images.githubusercontent.com/90787864/236683053-6f6790b9-0f93-49da-9197-8a04155e9714.png)


