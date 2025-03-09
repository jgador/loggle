from llama_index.core import (
    VectorStoreIndex,
    QueryBundle,
    Response,
    Settings,
)
from llama_index.embeddings.ollama import OllamaEmbedding
from llama_index.llms.ollama import Ollama
from index import es_vector_store
from pathlib import Path

# Configure endpoints and settings
OLLAMA_BASE_URL = "http://localhost:11434"
SIMILARITY_TOP_K = 3

def get_example_queries():
    """Get example queries based on conversations in the JSON file"""
    current_dir = Path(__file__).parent.parent.parent
    data_file = current_dir / "data" / "conversations.json"
    
    return [
        "Give me summary of water related issues"
    ]

def run_queries():
    # Initialize models
    llm = Ollama(
        model="tinyllama",
        base_url=OLLAMA_BASE_URL,
        timeout=30
    )
    
    embed_model = OllamaEmbedding(
        model_name="tinyllama",
        base_url=OLLAMA_BASE_URL
    )
    
    # Configure global settings
    Settings.llm = llm
    Settings.embed_model = embed_model
    
    # Create index
    index = VectorStoreIndex.from_vector_store(vector_store=es_vector_store)
    
    # Configure query engine
    query_engine = index.as_query_engine(
        similarity_top_k=SIMILARITY_TOP_K,
        response_mode="compact"
    )
    
    # Run queries
    queries = get_example_queries()
    for query in queries:
        print(f"\n{'='*50}")
        print(f"Query: {query}")
        print(f"{'='*50}")
        
        response = query_engine.query(query)
        
        print("\nResponse:")
        print(response)
        
        if hasattr(response, 'source_nodes'):
            print("\nSources:")
            for node in response.source_nodes:
                print(f"- Conversation ID: {node.node.metadata.get('conversation_id')}")
                print(f"  Text: {node.node.text[:200]}...")

if __name__ == "__main__":
    run_queries()