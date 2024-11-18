# About

This is a basic Exam Generator that uses OpenAI's ChatGPT API in the background to generate exams based on given parameters through a web interface.

# How to Run

## 1. If you don't already, install dotnet on your machine.

## 2. Clone the repo
```
$ git clone https://github.com/mehonal/exam-generator.git
```
## 3. Head over to the repo directory
```
$ cd exam-generator
```
## 4. Export OPENAI key as an environment variable

### Linux:
```
$ export OPENAI_API_KEY=sk-proj-your_key_goes_here
```

### Windows:
```
$ set OPENAI_API_KEY=sk-proj-your_key_goes_here
```

## 5. Run the project
```
$ dotnet run
```

Now just wait for the app to build and run, and you should be able to access it on `localhost:5009`. Enjoy!

# Screenshots 

![Exam Generator UI](https://raw.githubusercontent.com/mehonal/exam-generator/master/wwwroot/images/screenshots/exam-generator-ui.png)
![Sample Exam](https://raw.githubusercontent.com/mehonal/exam-generator/master/wwwroot/images/screenshots/sample-exam.png)
