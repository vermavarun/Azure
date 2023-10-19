def trigger_multiple_translation():
    from azure.core.credentials import AzureKeyCredential
    from azure.ai.translation.document import (
        DocumentTranslationClient,
        DocumentTranslationInput,
        TranslationTarget
    )

    endpoint = "https://{service-name}.cognitiveservices.azure.com/"
    key = "{key}"
    source_container_url = "{SAS_URL_WITH LIST_READ_PERMISSIONS}"
    target_container_url = "{SAS_URL_WITH LIST_READ_WRITE_CREATE_PERMISSIONS}"

    client = DocumentTranslationClient(endpoint, AzureKeyCredential(key))

    poller = client.begin_translation(inputs=[
            DocumentTranslationInput(
                source_url=source_container_url,
                targets=[
                    TranslationTarget(
                        target_url=target_container_url,
                        language="en"
                    )
                ]
            )
        ]
    )
    result = poller.result()

    print(f"Status: {poller.status()}")
    print(f"Created on: {poller.details.created_on}")
    print(f"Last updated on: {poller.details.last_updated_on}")
    print(f"Total number of translations on documents: {poller.details.documents_total_count}")

    print("\nOf total documents...")
    print(f"{poller.details.documents_failed_count} failed")
    print(f"{poller.details.documents_succeeded_count} succeeded")

    for document in result:
        print(f"Document ID: {document.id}")
        print(f"Document status: {document.status}")
        if document.status == "Succeeded":
            print(f"Source document location: {document.source_document_url}")
            print(f"Translated document location: {document.translated_document_url}")
            print(f"Translated to language: {document.translated_to}\n")
        elif document.error:
            print(f"Error Code: {document.error.code}, Message: {document.error.message}\n")
    # [END multiple_translation]


if __name__ == '__main__':
    trigger_multiple_translation()