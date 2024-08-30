FROM golang:1.22

WORKDIR /go/src/app

COPY . .

RUN go build -o app app.go

CMD ["./app"]